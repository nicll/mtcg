using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MtcgServer.Helpers.Random;

namespace MtcgServer
{
    public class MtcgServer
    {
        private const string Pepper = "mtcg--";
        private readonly ConcurrentDictionary<Session, Guid> _sessions;
        private volatile Player? _firstBattlingPlayer;
        private volatile BattleResult? _btlResult;
        private readonly SemaphoreSlim _invokeBattleLimiter, _invokeBattleExclusive, _invokeBattleHang;
        private readonly IDatabase _db;
        private readonly IBattleHandler _btl;
        private readonly CardStore _store;
        private readonly PackageStore _packages;

        public MtcgServer(IDatabase database, IBattleHandler battleHandler)
        {
            _sessions = new ConcurrentDictionary<Session, Guid>();
            _invokeBattleLimiter = new SemaphoreSlim(2, 2);
            _invokeBattleExclusive = new SemaphoreSlim(1, 1);
            _invokeBattleHang = new SemaphoreSlim(0, 1);
            _db = database;
            _btl = battleHandler;
            _store = new CardStore(database);
            _packages = new PackageStore(database);
        }

        /// <summary>
        /// Creates a new account for a player and logs them in.
        /// </summary>
        /// <param name="user">Username of the player.</param>
        /// <param name="pass">Password of the player.</param>
        /// <returns>Session of the player.</returns>
        public async Task<Session?> Register(string user, string pass)
        {
            // check if a player with this name already exists
            if (await _db.SearchPlayer(user) != Guid.Empty)
                return null;

            // create new player object and save it in the database
            var newId = Guid.NewGuid();
            var passHash = HashPlayerPassword(newId, pass);
            var newPlayer = CreateNewPlayer(newId, user, passHash);
            await _db.SavePlayer(newPlayer, PlayerChange.Everything);

            // also create a new login session and return it
            var newSession = new Session();
            _sessions.AddOrUpdate(newSession, newId, (_, __) => newId);
            return newSession;
        }

        /// <summary>
        /// Logs an already existing player in.
        /// </summary>
        /// <param name="user">Username of the player.</param>
        /// <param name="pass">Password of the player.</param>
        /// <returns>Session of the player.</returns>
        public async Task<Session?> Login(string user, string pass)
        {
            // get user from database
            var playerId = await _db.SearchPlayer(user);

            if (playerId == Guid.Empty)
                return null;

            // get player object and password hash ready
            var player = await _db.ReadPlayer(playerId);

            if (player is null)
                return null;

            var passHash = HashPlayerPassword(playerId, pass);

            // if hashes don't match don't create session
            if (!CompareHashes(player.PasswordHash, passHash))
                return null;

            // create login session, save it and return it to the client
            var newSession = new Session();
            _sessions.AddOrUpdate(newSession, playerId, (_, __) => playerId);
            return newSession;
        }

        /// <summary>
        /// Logs a logged-in player out.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <returns>Whether logout was successful or not.</returns>
        public bool Logout(Session session)
            => _sessions.TryRemove(session, out _);

        /// <summary>
        /// Causes a player to join a battle immediately.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        public async Task<BattleResult?> InvokeBattle(Session session)
        {
            if (await GetPlayer(session) is not Player player)
                return null;

            if (player.Deck.Count != 5)
                return null;

            // only two taks may run simultaneously
            await _invokeBattleLimiter.WaitAsync().ConfigureAwait(false);

            // restrict to serialized execution
            await _invokeBattleExclusive.WaitAsync();

            if (_firstBattlingPlayer is null) // first player connects
            {
                _firstBattlingPlayer = player;
                _invokeBattleExclusive.Release(); // second player may now join
                System.Diagnostics.Debug.Assert(_invokeBattleHang.CurrentCount == 1);
                await _invokeBattleHang.WaitAsync(); // wait until unlocked by second player

                // invoked after second player finished
                var resultCopy = _btlResult;
                _firstBattlingPlayer = null;
                _btlResult = null;

                // let others continue
                _invokeBattleExclusive.Release();
                _invokeBattleLimiter.Release(2);

                // finish first player
                return resultCopy;
            }

            System.Diagnostics.Debug.Assert(_invokeBattleExclusive.CurrentCount == 0);
            // finish second player
            var result = _btlResult = _btl.RunBattle(player, _firstBattlingPlayer);

            // let other player's task continue
            _invokeBattleHang.Release();
            return result;
        }

        /// <summary>
        /// Fetches all information about a player, if found.
        /// </summary>
        /// <param name="user">Username of the player.</param>
        /// <returns>Information about the player or <see langword="null"/>.</returns>
        public async Task<Player?> GetPlayer(string user)
        {
            var playerId = await _db.SearchPlayer(user);

            if (playerId == Guid.Empty)
                return null;

            return await _db.ReadPlayer(playerId);
        }

        /// <summary>
        /// Fetches all information about a player, if found.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <returns>Information about the player or <see langword="null"/>.</returns>
        public async Task<Player?> GetPlayer(Session session)
        {
            if (!TryGetPlayerID(session, out Guid playerId))
                return null;

            return await _db.ReadPlayer(playerId);
        }

        /// <summary>
        /// Updates the deck for a specific player.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <param name="cardIds">IDs of the cards of the new deck.</param>
        /// <returns>Whether the update was successful or not.</returns>
        public async Task<bool> SetDeck(Session session, ICollection<Guid> cardIds)
        {
            if (await GetPlayer(session) is not Player player)
                return false;

            if (cardIds.Count != 5)
                return false;

            player.Deck.Clear();

            foreach (var cardId in cardIds)
            {
                if (player.Stack.FirstOrDefault(c => c.Id == cardId) is not ICard card)
                    return false;

                player.Deck.Add(card);
            }

            await _db.SavePlayer(player, PlayerChange.Deck);
            return true;
        }

        /// <summary>
        /// Gets a collection of all currently available cards in the store.
        /// </summary>
        /// <returns>Collection of cards in the store.</returns>
        public async Task<IReadOnlyCollection<CardStoreEntry>> GetAvailableStoreCards()
        {
            if (!_store.Initialized)
                await _store.Update();

            return _store.GetAllCards();
        }

        /// <summary>
        /// Gets a collection of all cards that match the requirements for a specific card.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <param name="cardId">ID of the card.</param>
        /// <returns>Collection of eligible cards in the store.</returns>
        public async Task<IReadOnlyCollection<CardStoreEntry>?> GetEligibleStoreCards(Session session, Guid cardId)
        {
            if (await GetPlayer(session) is not Player player)
                return null;

            var card = await _db.ReadCard(cardId);

            if (player is null || card is null)
                return null;

            if (!player.Stack.Contains(card))
                return null;

            if (!_store.Initialized)
                await _store.Update();

            return _store.GetEligibleCards(card);
        }

        /// <summary>
        /// Marks a card in the player's inventory as tradable and places it in the store.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <param name="cardId">ID of the card.</param>
        /// <param name="requirements">Requirements when trading the card.</param>
        /// <returns>Whether the card was successfully pushed to the store.</returns>
        public async Task<bool> PushCardToStore(Session session, Guid cardId, ICollection<ICardRequirement> requirements)
        {
            if (await GetPlayer(session) is not Player player)
                return false;

            var card = await _db.ReadCard(cardId);

            if (player is null || card is null)
                return false;

            return await _store.PushCard(player, card, requirements);
        }

        /// <summary>
        /// Trades a player's card with a card from another player.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <param name="ownCardId">ID of the card of the current player.</param>
        /// <param name="otherCardId">ID of the other card that will be traded.</param>
        /// <returns>Whether the card could successfully be traded.</returns>
        public async Task<bool> TradeCards(Session session, Guid ownCardId, Guid otherCardId)
        {
            if (await GetPlayer(session) is not Player player)
                return false;

            var ownCard = await _db.ReadCard(ownCardId);
            var otherCard = await _db.ReadCard(otherCardId);

            if (player is null || ownCard is null || otherCard is null)
                return false;

            return await _store.TradeCard(player, ownCard, otherCard);
        }

        /// <summary>
        /// Gets all currently available packages.
        /// </summary>
        /// <returns>A collection of all packages.</returns>
        public async Task<IReadOnlyCollection<CardPackage>> GetAllPackages()
        {
            if (!_packages.Initialized)
                await _packages.Update();

            return _packages.GetPackages();
        }

        /// <summary>
        /// Gets all packages that a player can afford.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <returns>A collection of all affordable packages.</returns>
        public async Task<IReadOnlyCollection<CardPackage>?> GetAffordablePackages(Session session)
        {
            if (await GetPlayer(session) is not Player player)
                return null;

            if (!_packages.Initialized)
                await _packages.Update();

            return _packages.GetAffordablePackages(player.Coins);
        }

        /// <summary>
        /// Gets all packages below a certain price.
        /// </summary>
        /// <param name="maxPrice">The price threshold.</param>
        /// <returns>A collection of all affordable packages.</returns>
        public async Task<IReadOnlyCollection<CardPackage>> GetAffordablePackages(int maxPrice)
        {
            if (!_packages.Initialized)
                await _packages.Update();

            return _packages.GetAffordablePackages(maxPrice);
        }

        /// <summary>
        /// Registers a new package created by an admin.
        /// </summary>
        /// <param name="package">The new package.</param>
        public async Task<bool> RegisterPackage(Session session, CardPackage package)
        {
            if (await GetPlayer(session) is not Player)
                return false;

            // Authorization would happen here if it existed

            await _packages.RegisterPackage(package);
            return true;
        }

        /// <summary>
        /// Makes a player buy randomly chosen cards.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        public async Task BuyRandomCards(Session session, int price = 5)
        {
            if (await GetPlayer(session) is not Player player)
                return;

            if (player.Coins - price < 0)
                return;

            if (!_packages.Initialized)
                await _packages.Update();

            var newPlayer = player with { Coins = player.Coins - price };
            _packages.GetRandomCards(5).ForEach(c => newPlayer.Stack.Add(c.CollissionlessDuplicate()));

            await _db.SavePlayer(newPlayer, PlayerChange.AfterBuyPackage);
        }

        /// <summary>
        /// Makes a player buy a randomly chosen package that they can afford.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        public async Task BuyRandomPackage(Session session)
        {
            if (await GetPlayer(session) is not Player player)
                return;

            if (!_packages.Initialized)
                await _packages.Update();

            var possiblePackages = _packages.GetPackages().Where(p => p.Price <= player.Coins).ToArray();

            if (possiblePackages.Length < 1)
                return;

            var pickedPackage = possiblePackages[_rnd.Next(possiblePackages.Length)];
            var newPlayer = player with { Coins = player.Coins - pickedPackage.Price };

            foreach (var card in pickedPackage.Cards)
                newPlayer.Stack.Add(card.CollissionlessDuplicate());

            await _db.SavePlayer(newPlayer, PlayerChange.AfterBuyPackage);
        }

        /// <summary>
        /// Makes a player buy a specific package if they can afford it.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <param name="packageId">ID of the package.</param>
        public async Task BuySpecificPackage(Session session, Guid packageId)
        {
            if (await GetPlayer(session) is not Player player)
                return;

            if (!_packages.Initialized)
                await _packages.Update();

            var package = _packages.GetPackage(packageId);

            if (package is null)
                return;

            var newPlayer = player with { Coins = player.Coins - package.Price };

            foreach (var card in package.Cards)
                newPlayer.Stack.Add(card.CollissionlessDuplicate());

            await _db.SavePlayer(newPlayer, PlayerChange.AfterBuyPackage);
        }

        /// <summary>
        /// Attempts to get the player's ID for a session.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        /// <param name="playerId">ID of the player or <see cref="Guid.Empty"/>.</param>
        /// <returns>Whether or not the player's ID was found.</returns>
        private bool TryGetPlayerID(in Session session, out Guid playerId)
        {
            if (_sessions.TryGetValue(session, out playerId))
                return true;

            playerId = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Creates a new player with default stats.
        /// </summary>
        /// <param name="id">ID of the player.</param>
        /// <param name="name">Name of the player.</param>
        /// <param name="passwordHash">Hashed password of the player.</param>
        /// <returns></returns>
        private static Player CreateNewPlayer(Guid id, string name, byte[] passwordHash)
            => new Player(id, name, passwordHash, string.Empty, string.Empty, 20, Array.Empty<ICard>(), Array.Empty<ICard>(), 100, 0, 0);

        /// <summary>
        /// Hashes a player's password.
        /// </summary>
        /// <param name="playerId">ID of the player.</param>
        /// <param name="pass">Password of the player.</param>
        /// <returns>Hashed password of the player.</returns>
        private static byte[] HashPlayerPassword(Guid playerId, in string pass)
        {
            var hasher = SHA256.Create();
            var combination = Encoding.UTF8.GetBytes(Pepper + playerId.ToString("N") + pass);
            return hasher.ComputeHash(combination);
        }

        /// <summary>
        /// Compare two memory areas for equal content.
        /// Basically like memcmp().
        /// </summary>
        /// <param name="left">First memory area.</param>
        /// <param name="right">Second memory area.</param>
        /// <returns>Whether or not the two areas contain the same content.</returns>
        private static bool CompareHashes(in ReadOnlySpan<byte> left, in ReadOnlySpan<byte> right)
            => left.SequenceEqual(right);
    }
}

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
        private readonly List<CardPackage> _packages;
        private readonly ConcurrentDictionary<Session, Guid> _sessions;
        private readonly List<Guid> _waiting;
        private readonly object _lobbyLock;
        private volatile Player? _firstBattlingPlayer;
        private volatile BattleResult? _btlResult;
        private readonly SemaphoreSlim _invokeBattleLimiter, _invokeBattleExclusive, _invokeBattleHang;
        private readonly IDatabase _db;
        private readonly IMatchmaker _mm;
        private readonly IBattleHandler _btl;

        public MtcgServer(IDatabase database, IMatchmaker matchmaker, IBattleHandler battleHandler)
        {
            _packages = new List<CardPackage>();
            _sessions = new ConcurrentDictionary<Session, Guid>();
            _waiting = new List<Guid>();
            _lobbyLock = new object();
            _invokeBattleLimiter = new SemaphoreSlim(0, 2);
            _invokeBattleExclusive = new SemaphoreSlim(0, 1);
            _invokeBattleHang = new SemaphoreSlim(1, 1);
            _db = database;
            _mm = matchmaker;
            _btl = battleHandler;
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
        {
            DisableBattle(session);
            return _sessions.TryRemove(session, out _);
        }

        /// <summary>
        /// Marks a player as available for battling.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        public void EnableBattle(Session session)
        {
            if (!TryGetPlayerID(session, out Guid playerId))
                return;

            lock (_lobbyLock)
            {
                if (!_waiting.Contains(playerId))
                    _waiting.Add(playerId);
            }
        }

        /// <summary>
        /// Marks a player as unavailable for battling.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        public void DisableBattle(Session session)
        {
            if (!TryGetPlayerID(session, out Guid playerId))
                return;

            lock (_lobbyLock)
                _waiting.Remove(playerId);
        }

        /// <summary>
        /// Causes a player to join a battle ASAP.
        /// </summary>
        /// <param name="session">Session of the player.</param>
        public async Task<BattleResult?> InvokeBattle(Session session)
        {
            if (!TryGetPlayerID(session, out Guid playerId))
                return null;

            var player = await _db.ReadPlayer(playerId);

            if (player?.Deck.Count != 5)
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
        /// <param name="session">Session of the player.</param>
        /// <returns>Information about the player or <see langword="null"/>.</returns>
        public async Task<Player?> GetPlayer(Session session)
        {
            if (!TryGetPlayerID(session, out Guid playerId))
                return null;

            return await _db.ReadPlayer(playerId);
        }

        /// <summary>
        /// Registers a new package created by an admin.
        /// </summary>
        /// <param name="package">The new package.</param>
        public void RegisterPackage(CardPackage package)
            => _packages.Add(package);

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

            var newPlayer = player with { Coins = player.Coins - price };
            RetrieveRandomCards(5).ForEach(c => newPlayer.Stack.Add(c));

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

            var possiblePackages = _packages.Where(p => p.Price <= player.Coins).ToArray();

            if (possiblePackages.Length < 1)
                return;

            var pickedPackage = possiblePackages[_rnd.Next(possiblePackages.Length)];
            var newPlayer = player with { Coins = player.Coins - pickedPackage.Price };

            foreach (var card in pickedPackage.Cards)
                newPlayer.Stack.Add(card);

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

            var package = _packages.Find(p => p.Id == packageId);

            if (package is null)
                return;

            var newPlayer = player with { Coins = player.Coins - package.Price };

            foreach (var card in package.Cards)
                newPlayer.Stack.Add(card);

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
        /// Retrieves the specified number of randomly chosen cards
        /// from the existing packages.
        /// </summary>
        /// <param name="count"></param>
        /// <returns>The randomly chosen cards.</returns>
        private List<ICard> RetrieveRandomCards(int count)
        {
            if (_packages.Count == 0)
                throw new InvalidOperationException("Tried to retrieve randomly chosen cards when none were defined.");

            var cards = new List<ICard>(count);

            for (int i = 0; i < count; ++i)
            {
                var package = _packages[_rnd.Next(_packages.Count)];
                cards[i] = package.Cards[_rnd.Next(package.Cards.Count)];
            }

            return cards;
        }

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

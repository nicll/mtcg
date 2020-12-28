using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MtcgServer
{
    /// <summary>
    /// Represents a store that holds tradable cards.
    /// </summary>
    public class CardStore
    {
        private readonly IDatabase _db;
        private List<CardStoreEntry> _cache;
        private readonly SemaphoreSlim _lock;

        public bool Initialized => _cache is not null;

        public CardStore(IDatabase db)
        {
            _db = db;
            _cache = new List<CardStoreEntry>();
            _lock = new SemaphoreSlim(1);
        }

        public async Task Update()
        {
            await _lock.WaitAsync();

            _cache = (await _db.ReadStore()).ToList();

            _lock.Release();
        }

        public IReadOnlyCollection<CardStoreEntry> GetAllCards()
            => _cache.AsReadOnly();

        public IReadOnlyCollection<CardStoreEntry> GetEligibleCards(ICard match)
            => _cache.FindAll(e => e.Requirements.All(r => r.CheckRequirement(match)));

        public async Task<bool> PushCard(Player first, ICard card, ICollection<ICardRequirement> requirements)
        {
            if (first.Deck.Contains(card) || !first.Stack.Contains(card))
                return false;

            await _db.AddToStore(first, card, requirements);
            await Update();
            return true;
        }

        public async Task<bool> TradeCard(Player second, ICard own, ICard other)
        {
            if (second.Deck.Contains(own) || !second.Stack.Contains(other))
                return false;

            var first = await _db.FindOwner(other);

            second.Stack.Remove(own);
            second.Stack.Add(other);

            first.Stack.Remove(other);
            first.Stack.Add(own);

            await _db.SavePlayer(second, PlayerChange.AfterTrade);
            await _db.SavePlayer(first, PlayerChange.AfterTrade);

            await Update();
            return true;
        }
    }
}

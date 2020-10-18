using System;
using System.Collections.Generic;

namespace MtcgServer
{
    public class Player
    {
        private readonly List<Card> _stack = new List<Card>();
        private readonly List<Card> _deck = new List<Card>(4);

        public Guid ID { get; }

        public string Name { get; }

        public byte[] PasswordHash { get; }

        public int Coins { get; }

        public IReadOnlyCollection<Card> Stack => _stack.AsReadOnly();

        public IReadOnlyCollection<Card> Deck => _deck.AsReadOnly();

        public int ELO { get; }

        public int Wins { get; }

        public int Losses { get; }

        public Player(Guid id, string name, byte[] passwordHash, int coins, IReadOnlyCollection<Card> stack, IReadOnlyCollection<Card> deck, int elo, int wins, int losses)
        {
            ID = id;
            Name = name;
            PasswordHash = passwordHash;
            Coins = coins;
            _stack = new List<Card>(stack);
            _deck = new List<Card>(deck);
            ELO = elo;
            Wins = wins;
            Losses = losses;
        }

        internal static Player CreateNewPlayer(Guid id, string name, byte[] passwordHash)
            => new Player(id, name, passwordHash, 20, new Card[0], new Card[0], 100, 0, 0);
    }
}

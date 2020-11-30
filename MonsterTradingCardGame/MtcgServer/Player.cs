using System;
using System.Collections.Generic;

namespace MtcgServer
{
    /// <summary>
    /// Represents a player, their inventory and stats.
    /// </summary>
    public class Player
    {
        private readonly List<ICard> _stack = new List<ICard>();
        private readonly List<ICard> _deck = new List<ICard>(4);

        public Guid ID { get; }

        public string Name { get; }

        public byte[] PasswordHash { get; }

        public string StatusText { get; }

        public string EmoticonText { get; }

        public int Coins { get; }

        public IReadOnlyCollection<ICard> Stack => _stack.AsReadOnly();

        public IReadOnlyCollection<ICard> Deck => _deck.AsReadOnly();

        public int ELO { get; }

        public int Wins { get; }

        public int Losses { get; }

        public Player(Guid id, string name, byte[] passwordHash, string statusText, string emoticonText, int coins,
            IReadOnlyCollection<ICard> stack, IReadOnlyCollection<ICard> deck, int elo, int wins, int losses)
        {
            ID = id;
            Name = name;
            PasswordHash = passwordHash;
            StatusText = statusText;
            EmoticonText = emoticonText;
            Coins = coins;
            _stack = new List<ICard>(stack);
            _deck = new List<ICard>(deck);
            ELO = elo;
            Wins = wins;
            Losses = losses;
        }

        internal static Player CreateNewPlayer(Guid id, string name, byte[] passwordHash)
            => new Player(id, name, passwordHash, string.Empty, string.Empty, 20, new ICard[0], new ICard[0], 100, 0, 0);
    }
}

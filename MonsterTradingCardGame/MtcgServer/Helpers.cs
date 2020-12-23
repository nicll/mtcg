using System;
using System.Collections.Generic;

namespace MtcgServer
{
    /// <summary>
    /// This class contains various helper functions.
    /// </summary>
    internal static class Helpers
    {
        internal static class Random
        {
            internal static readonly System.Random _rnd = new();

            /// <summary>
            /// Chooses a random card from a list of available cards.
            /// </summary>
            /// <param name="cards">The list of available cards.</param>
            /// <returns>The chosen card.</returns>
            internal static ICard ChooseRandomCard(List<ICard> cards)
                => cards[_rnd.Next(cards.Count)];

            /// <summary>
            /// Chooses a random card from a list of available cards and removes it.
            /// Then returns the chosen card.
            /// </summary>
            /// <param name="cards">The list of available cards.</param>
            /// <returns>The chosen card.</returns>
            internal static ICard PopRandomCard(List<ICard> cards)
            {
                var card = ChooseRandomCard(cards);
                cards.Remove(card);
                return card;
            }
        }
    }
}

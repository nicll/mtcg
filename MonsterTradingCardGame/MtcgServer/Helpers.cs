using System;
using System.Collections.Generic;

namespace MtcgServer
{
    /// <summary>
    /// This class contains various helper functions.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// This class contains helpful methods and fields when working with randomness.
        /// </summary>
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

        /// <summary>
        /// This class provides additional features that should be provided by the framework but aren't (yet).
        /// </summary>
        internal static class LanguageExtensions
        {
            /// <summary>
            /// An implementation of discriminated unions for 2 types.
            /// </summary>
            /// <typeparam name="A">First type.</typeparam>
            /// <typeparam name="B">Second type.</typeparam>
            public abstract class Union2<A, B>
            {
                public abstract T Match<T>(Func<A, T> f, Func<B, T> g);

                private Union2() { }

                public sealed class Case1 : Union2<A, B>
                {
                    public readonly A Item;
                    public Case1(A item) : base() { this.Item = item; }
                    public override T Match<T>(Func<A, T> f, Func<B, T> g)
                    {
                        return f(Item);
                    }
                }

                public sealed class Case2 : Union2<A, B>
                {
                    public readonly B Item;
                    public Case2(B item) { this.Item = item; }
                    public override T Match<T>(Func<A, T> f, Func<B, T> g)
                    {
                        return g(Item);
                    }
                }
            }

            /// <summary>
            /// An implementation of discriminated unions for 3 types.
            /// </summary>
            /// <typeparam name="A">First type.</typeparam>
            /// <typeparam name="B">Second type.</typeparam>
            /// <typeparam name="C">Third type.</typeparam>
            // shamelessly stolen from https://stackoverflow.com/a/3199453/13282284
            public abstract class Union3<A, B, C>
            {
                public abstract T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h);
                // private ctor ensures no external classes can inherit
                private Union3() { }

                public sealed class Case1 : Union3<A, B, C>
                {
                    public readonly A Item;
                    public Case1(A item) : base() { this.Item = item; }
                    public override T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h)
                    {
                        return f(Item);
                    }
                }

                public sealed class Case2 : Union3<A, B, C>
                {
                    public readonly B Item;
                    public Case2(B item) { this.Item = item; }
                    public override T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h)
                    {
                        return g(Item);
                    }
                }

                public sealed class Case3 : Union3<A, B, C>
                {
                    public readonly C Item;
                    public Case3(C item) { this.Item = item; }
                    public override T Match<T>(Func<A, T> f, Func<B, T> g, Func<C, T> h)
                    {
                        return h(Item);
                    }
                }
            }
        }
    }
}

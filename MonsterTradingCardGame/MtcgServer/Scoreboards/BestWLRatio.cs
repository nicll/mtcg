using System;

namespace MtcgServer.Scoreboards
{
    public class BestWLRatio : IScoreboard
    {
        public int Compare(Player? x, Player? y)
            => x is Player px && y is Player py
                ? -((double)px.Wins / px.Losses).CompareTo((double)py.Wins / py.Losses)
                : default;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MtcgServer
{
    public abstract class BattleResult
    {
        public IList<string> LogEntries { get; }

        protected BattleResult(IList<string> log)
            => LogEntries = log;

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var entry in LogEntries)
                sb.AppendLine(entry);

            sb.Length -= Environment.NewLine.Length;
            return sb.ToString();
        }

        public class Winner : BattleResult
        {
            public Player WinningPlayer { get; }

            public Player LosingPlayer { get; }

            public Winner(Player winningPlayer, Player losingPlayer, IList<string> log) : base(log)
            {
                WinningPlayer = winningPlayer;
                LosingPlayer = losingPlayer;
            }

            public override string ToString()
                => $"{WinningPlayer.Name} won this battle. {LosingPlayer.Name} lost this battle."
                    + Environment.NewLine + base.ToString();
        }

        public class Draw : BattleResult
        {
            public Draw(IList<string> log) : base(log)
            {
            }

            public override string ToString()
                => "This battle resulted in a draw." + Environment.NewLine + base.ToString();
        }
    }
}

using System;

namespace MtcgServer
{
    public interface IBattleHandler
    {
        BattleResult RunBattle(Player p1, Player p2);
    }
}

using System;

namespace MtcgServer
{
    public interface IDatabase
    {
        Guid SearchPlayer(string name);

        Player ReadPlayer(Guid id);

        void SavePlayer(Player player, PlayerChange changes);
    }
}

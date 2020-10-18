using System;

namespace MtcgServer
{
    public class PostgreSqlDatabase : IDatabase
    {
        public Player ReadPlayer(Guid id)
        {
            throw new NotImplementedException();
        }

        public void SavePlayer(Player player, PlayerChange changes)
        {
            throw new NotImplementedException();
        }

        public Guid SearchPlayer(string name)
        {
            throw new NotImplementedException();
        }
    }
}

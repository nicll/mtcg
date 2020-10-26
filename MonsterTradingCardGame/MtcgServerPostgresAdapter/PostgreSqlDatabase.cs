using System;
using System.Dynamic;
using Npgsql;

namespace MtcgServer.Databases.Postgres
{
    public class PostgreSqlDatabase : IDatabase
    {
        private readonly string _connectionString;

        public PostgreSqlDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

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
            var conn = OpenConnection();
            var cmd = new NpgsqlCommand("SELECT id FROM users WHERE name = @name", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Prepare();

            var result = cmd.ExecuteScalar();
            CloseConnection(conn);

            if (result is Guid playerId)
                return playerId;

            return Guid.Empty;
        }

        private NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private void CloseConnection(NpgsqlConnection connection)
            => connection.Close();
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace MtcgServer.Databases.Postgres
{
    public class PostgreSqlDatabase : IDatabase
    {
        private readonly string _connectionString;

        static PostgreSqlDatabase()
        {
            // maps the locally defined enum ElementType to the one defined in the database
            NpgsqlConnection.GlobalTypeMapper.MapEnum<ElementType>("element_type");
            // as monster_type does not have a locally defined equivalent we have to map is manually
        }

        public PostgreSqlDatabase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async ValueTask<Player?> ReadPlayer(Guid id)
        {
            using var conn = OpenConnection();
            string name, statusText, emoticon;
            byte[] pwHash;
            int coins, elo, wins, losses;
            List<ICard> stackCards = new(), deckCards = new();

            using (var cmd = new NpgsqlCommand("SELECT name, pass_hash, statusmsg, emoticon, coins, elo, wins, losses FROM users WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.PrepareAsync();
                var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                name = reader.GetString(0);
                pwHash = await reader.GetFieldValueAsync<byte[]>(1);
                statusText = reader.GetString(2);
                emoticon = reader.GetString(3);
                coins = reader.GetInt32(4);
                elo = reader.GetInt32(5);
                wins = reader.GetInt32(6);
                losses = reader.GetInt32(7);
            }

            using (var cmd = new NpgsqlCommand("SELECT card_id, damage, element_type, monster_type FROM stacks JOIN users ON user_id = users.id JOIN cards ON card_id = cards.id WHERE user_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.PrepareAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var cardId = reader.GetGuid(0);
                    var damage = reader.GetInt32(1);
                    var elementType = await reader.GetFieldValueAsync<ElementType>(2);
                    var monsterType = await reader.GetFieldValueAsync<string>(3);
                    stackCards.Add(CreateCardFromData(cardId, damage, elementType, monsterType));
                }
            }

            using (var cmd = new NpgsqlCommand("SELECT card_id, damage, element_type, monster_type FROM decks JOIN users ON user_id = users.id JOIN cards ON card_id = cards.id WHERE user_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.PrepareAsync();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var cardId = reader.GetGuid(0);
                    var damage = reader.GetInt32(1);
                    var elementType = await reader.GetFieldValueAsync<ElementType>(2);
                    var monsterType = await reader.GetFieldValueAsync<string>(3);
                    deckCards.Add(CreateCardFromData(cardId, damage, elementType, monsterType));
                }
            }

            return new Player(id, name, pwHash, statusText, emoticon, coins, stackCards, deckCards, elo, wins, losses);
        }

        public async ValueTask SavePlayer(Player player, PlayerChange changes)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<Guid> SearchPlayer(string name)
        {
            var conn = OpenConnection();
            using var cmd = new NpgsqlCommand("SELECT id FROM users WHERE name = @name", conn);
            cmd.Parameters.AddWithValue("@name", name);
            await cmd.PrepareAsync();

            var result = await cmd.ExecuteScalarAsync();

            if (result is Guid playerId)
                return playerId;

            return Guid.Empty;
        }

        public async ValueTask<ICard?> ReadCard(Guid id)
        {
            using var conn = OpenConnection();
            using var cmd = new NpgsqlCommand("SELECT damage, element_type, monster_type FROM cards WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.PrepareAsync();

            var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var damage = reader.GetInt32(0);
                var elementType = await reader.GetFieldValueAsync<ElementType>(1);
                var monsterType = await reader.GetFieldValueAsync<string>(2);

                return CreateCardFromData(id, damage, elementType, monsterType);
            }

            return null;
        }

        public async ValueTask CreateCardInstance(ICard card)
        {
            using var conn = OpenConnection();
            using var cmd = new NpgsqlCommand("INSERT INTO cards VALUES (@id, @damage, @element_type, @monster_type)", conn);
            cmd.Parameters.AddWithValue("@id", card.Id);
            cmd.Parameters.AddWithValue("@damage", card.Damage);
            cmd.Parameters.AddWithValue("@element_type", card.Type);
            cmd.Parameters.AddWithValue("@monster_type", card is Cards.SpellCard ? "spell" : card.GetType().Name.ToLowerInvariant());
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
        }

        private static ICard CreateCardFromData(Guid id, int damage, ElementType elementType, string monsterType)
            => monsterType switch
        {
            "spell" => elementType switch
            {
                ElementType.Normal => new Cards.SpellCards.NormalSpell() { Id = id, Damage = damage },
                ElementType.Water => new Cards.SpellCards.WaterSpell() { Id = id, Damage = damage },
                ElementType.Fire => new Cards.SpellCards.FireSpell() { Id = id, Damage = damage },
                _ => throw new DatabaseException("Invalid spell element type found in database.")
            },
            "dragon" => new Cards.MonsterCards.Dragon() { Id = id, Damage = damage },
            "elf" => new Cards.MonsterCards.FireElf() { Id = id, Damage = damage },
            "goblin" => new Cards.MonsterCards.Goblin() { Id = id, Damage = damage },
            "knight" => new Cards.MonsterCards.Knight() { Id = id, Damage = damage },
            "kraken" => new Cards.MonsterCards.Kraken() { Id = id, Damage = damage },
            "ork" => new Cards.MonsterCards.Ork() { Id = id, Damage = damage },
            "wizard" => new Cards.MonsterCards.Wizard() { Id = id, Damage = damage },
            _ => throw new DatabaseException("Invalid monster type found in database.")
        };

        private NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            // manual closing is not required whenever the using keyword is used
            return conn;
        }
    }
}

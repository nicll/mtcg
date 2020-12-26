using System;
using System.Threading.Tasks;

namespace MtcgServer
{
    public interface IDatabase
    {
        ValueTask<Guid> SearchPlayer(string name);

        ValueTask<Player?> ReadPlayer(Guid id);

        ValueTask SavePlayer(Player player, PlayerChange changes);

        ValueTask<ICard?> ReadCard(Guid id);

        ValueTask CreateCardInstance(ICard card);
    }
}

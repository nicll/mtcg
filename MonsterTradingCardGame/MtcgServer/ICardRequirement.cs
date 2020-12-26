using System;

namespace MtcgServer
{
    public interface ICardRequirement
    {
        bool CheckRequirement(ICard card);
    }
}

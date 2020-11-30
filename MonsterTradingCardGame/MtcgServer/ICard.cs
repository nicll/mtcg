using System;

namespace MtcgServer
{
    public interface ICard
    {
        ElementType Type { get; }

        int Damage { get; init; }

        int CalculateDamage(in ICard other);
    }
}

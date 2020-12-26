using System;

namespace MtcgServer
{
    public interface ICard
    {
        Guid Id { get; }

        ElementType Type { get; }

        int Damage { get; }

        int CalculateDamage(in ICard other);
    }
}

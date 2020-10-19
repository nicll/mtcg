using System;

namespace MtcgServer
{
    public abstract class Card
    {
        public abstract ElementType Type { get; }

        public abstract int Damage { get; }

        public int CalculateDamage(in Card other) => (Type, other.Type) switch
        {
            (ElementType.Water,  ElementType.Fire)   => _CalculateDamage(other) * 2,
            (ElementType.Fire,   ElementType.Normal) => _CalculateDamage(other) * 2,
            (ElementType.Normal, ElementType.Water)  => _CalculateDamage(other) * 2,
            _ => _CalculateDamage(other)
        };

        protected virtual int _CalculateDamage(in Card other)
            => Damage;
    }
}

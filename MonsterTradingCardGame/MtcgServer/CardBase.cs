using System;

namespace MtcgServer
{
    public abstract class CardBase : ICard
    {
        public abstract ElementType Type { get; }

        public abstract int Damage { get; init; }

        public int CalculateDamage(in ICard other)
            => this is Cards.MonsterCard && other is Cards.MonsterCard
                ? _CalculateDamage(other) : (Type, other.Type) switch
                {
                    (ElementType.Water,  ElementType.Fire)   => _CalculateDamage(other) * 2,
                    (ElementType.Fire,   ElementType.Normal) => _CalculateDamage(other) * 2,
                    (ElementType.Normal, ElementType.Water)  => _CalculateDamage(other) * 2,
                    _ => _CalculateDamage(other)
                };

        protected virtual int _CalculateDamage(in ICard other)
            => Damage;

        public override string ToString()
            => GetType().Name + " (" + Type + ")";
    }
}

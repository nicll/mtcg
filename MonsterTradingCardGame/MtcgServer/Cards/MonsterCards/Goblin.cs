using System;

namespace MtcgServer.Cards.MonsterCards
{
    public class Goblin : MonsterCard
    {
        public override ElementType Type => ElementType.Normal;

        public override int Damage => 20;

        protected override int _CalculateDamage(in Card other)
            => other is Dragon
                ? 0
                : base._CalculateDamage(other);
    }
}

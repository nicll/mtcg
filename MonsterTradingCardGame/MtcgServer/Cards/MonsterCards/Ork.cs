using System;

namespace MtcgServer.Cards.MonsterCards
{
    public class Ork : MonsterCard
    {
        public override ElementType Type => ElementType.Normal;

        public override int Damage => 20;

        protected override int _CalculateDamage(in Card other)
            => other is Wizard
                ? 0
                : base._CalculateDamage(other);
    }
}

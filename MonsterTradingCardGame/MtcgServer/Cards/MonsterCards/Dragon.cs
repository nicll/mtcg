using System;

namespace MtcgServer.Cards.MonsterCards
{
    public class Dragon : MonsterCard
    {
        public override ElementType Type => ElementType.Fire;

        public override int Damage => 20;

        protected override int _CalculateDamage(in Card other)
            => other is FireElf
                ? 0
                : base._CalculateDamage(other);
    }
}

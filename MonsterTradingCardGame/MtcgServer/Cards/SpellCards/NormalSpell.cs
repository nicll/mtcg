using System;

namespace MtcgServer.Cards.SpellCards
{
    public class NormalSpell : SpellCard
    {
        public override ElementType Type => ElementType.Normal;

        public override int Damage => 20;
    }
}

using MtcgServer.Cards.MonsterCards;
using System;

namespace MtcgServer.Cards
{
    public abstract class SpellCard : Card
    {
        protected override int _CalculateDamage(in Card other)
            => other is Kraken
                ? 0
                : base._CalculateDamage(other);
    }
}

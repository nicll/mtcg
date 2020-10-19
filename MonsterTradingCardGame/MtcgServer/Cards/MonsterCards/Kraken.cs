using System;

namespace MtcgServer.Cards.MonsterCards
{
    public class Kraken : MonsterCard
    {
        public override ElementType Type => ElementType.Water;

        public override int Damage => 16;
    }
}

﻿using System;

namespace MtcgServer.Cards.MonsterCards
{
    public class Knight : MonsterCard
    {
        public override ElementType Type => ElementType.Normal;

        public override int Damage => 20;
    }
}

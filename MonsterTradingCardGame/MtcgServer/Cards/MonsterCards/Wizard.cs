﻿using System;

namespace MtcgServer.Cards.MonsterCards
{
    public class Wizard : MonsterCard
    {
        public override ElementType Type => ElementType.Normal;

        public override int Damage { get; init; } = 16;
    }
}

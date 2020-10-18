using System;

namespace MtcgServer
{
    [Flags]
    public enum PlayerChange : byte
    {
        Name = 1,
        Password = 2,
        Stack = 4,
        Deck = 8,
        Coins = 16,
        ELO = 32,
        Wins = 64,
        Losses = 128,
        Everything = 255
    }
}

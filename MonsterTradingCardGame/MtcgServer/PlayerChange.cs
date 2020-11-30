using System;

namespace MtcgServer
{
    [Flags]
    public enum PlayerChange : ushort
    {
        Name =          1 << 0,
        Password =      1 << 1,
        StatusText =    1 << 2,
        EmoticonText =  1 << 3,
        Stack =         1 << 4,
        Deck =          1 << 5,
        Coins =         1 << 6,
        ELO =           1 << 7,
        Wins =          1 << 8,
        Losses =        1 << 9,

        AfterGame = ELO | Wins | Losses,
        AfterBuyPackage = Stack | Coins,
        ModifiedDeck = Deck,

        Everything = (1 << 10) - 1
    }
}

using System;

namespace MtcgServer.CardRequirements
{
    public class IsMonsterCardRequirement : ICardRequirement
    {
        public bool CheckRequirement(ICard card)
            => card is Cards.MonsterCard;
    }
}

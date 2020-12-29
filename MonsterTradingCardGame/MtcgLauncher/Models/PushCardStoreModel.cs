using MtcgServer;
using System;
using System.Collections.Generic;

namespace MtcgLauncher.Models
{
    internal class PushCardStoreModel
    {
        public Guid CardId { get; set; }

        public ICollection<CardRequirementModel>? Requirements { get; set; }
    }
}

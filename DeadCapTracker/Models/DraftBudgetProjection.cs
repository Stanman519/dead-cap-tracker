using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeadCapTracker.Models
{
    public class DraftBudgetProjection
    {
        public int OwnerId { get; set; }
        public List<DraftPickTranslation> Picks { get; set; }
        public int PotentialSavings { get; set; } 
        public int RawBudget { get; set; }

        public DraftBudgetProjection()
        {

        }
        public DraftBudgetProjection(int ownerId, List<DraftPickTranslation> picks)
        {
            OwnerId = ownerId;
            Picks = picks;
            RawBudget = picks.Sum(p => p.SlotCost);
            PotentialSavings = (int)Math.Ceiling(picks.OrderByDescending(p => p.SlotCost).Select(_ => _.SlotCost * (1 - Utils.TaxiSquadCapHit)).Take(Utils.TaxiSquadSlots).Sum());
        }

    }
}
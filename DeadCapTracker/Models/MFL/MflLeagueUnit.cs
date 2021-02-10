using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class MflLeagueUnit
    {
        public string Unit { get; set; }
        public List<MflPlayer> Player { get; set; }
    }
}
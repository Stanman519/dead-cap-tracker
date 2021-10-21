using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class MflSalaries
    {
        public MflLeagueUnit LeagueUnit { get; set; }
    }
    public class MflSalariesParent
    {
        public MflSalaries Salaries { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }
    public class MflLeagueUnit
    {
        public string Unit { get; set; }
        public List<MflPlayer> Player { get; set; }
    }
}
using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class RosterPlayer
    {
        public string contractYear { get; set; }
        public string status { get; set; }
        public string id { get; set; }
        public string salary { get; set; }
        public string name { get; set; }
        public string owner { get; set; }
    }

    public class FranchiseRoster
    {
        public string week { get; set; }
        public List<RosterPlayer> player { get; set; }
        public string id { get; set; }
    }

    public class Rosters
    {
        public List<FranchiseRoster> franchise { get; set; }
    }

    public class MflRostersRoot
    {
        public Rosters rosters { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }
}
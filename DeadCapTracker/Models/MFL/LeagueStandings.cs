using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class MflStandingsParent
    {
        public string Version { get; set; }
        public LeagueStandings LeagueStandings { get; set; }
        public string Encoding { get; set; }
    }
    public class LeagueStandings
    {
        public List<MflFranchiseStandings> Franchise { set; get; }
    }
    public class MflFranchiseStandings
    {
        public string id { get; set; }
        public string pf { get; set; }
        public string h2hw { get; set; }
        public string h2hl { get; set; }
        public string vp { get; set; }
        // public string all_play_w { get; set; }
        // public string all_play_t { get; set; }
        // public string all_play_l { get; set; }
    }
}
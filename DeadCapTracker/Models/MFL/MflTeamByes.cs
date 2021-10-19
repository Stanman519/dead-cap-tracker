using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class TeamBye
    {
        public string bye_week { get; set; }
        public string id { get; set; }
    }

    public class NflByeWeeks
    {
        public string week { get; set; }
        public List<TeamBye> team { get; set; }
        public string year { get; set; }
    }

    public class MflByesRoot
    {
        public string version { get; set; }
        public NflByeWeeks nflByeWeeks { get; set; }
        public string encoding { get; set; }
    }

}
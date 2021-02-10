namespace DeadCapTracker.Models.MFL
{
    public class MflAgentsParent
    {
        public MflFreeAgents freeAgents { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }

    public class MflFreeAgents
    {
        public MflLeagueUnit LeagueUnit { get; set; }
    }
}
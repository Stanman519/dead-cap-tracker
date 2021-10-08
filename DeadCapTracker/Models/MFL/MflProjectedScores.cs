using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class ProjectedPlayerScore
    {
        public string score { get; set; }
        public string id { get; set; }
    }

    public class ProjectedScores
    {
        public string week { get; set; }
        public List<ProjectedPlayerScore> playerScore { get; set; }
    }

    public class ProjectedScoresRoot
    {
        public ProjectedScores projectedScores { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }
}
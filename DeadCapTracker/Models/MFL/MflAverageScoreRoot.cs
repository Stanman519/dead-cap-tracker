using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class PlayerAvgScore
    {
        public string isAvailable { get; set; }
        public string week { get; set; }
        public string score { get; set; }
        public string id { get; set; }
    }

    public class PlayerAvgScores
    {
        public string week { get; set; }
        public List<PlayerAvgScore> playerScore { get; set; }
    }

    public class MflAverageScoreRoot
    {
        public PlayerAvgScores playerScores { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }


}
using System.Collections.Generic;
using DeadCapTracker.Models.BotModels;

namespace DeadCapTracker.Models.MFL
{
    public class LiveScorePlayer
    {
        public string gameSecondsRemaining { get; set; }
        public string status { get; set; }
        public string updatedStats { get; set; }
        public string score { get; set; }
        public string id { get; set; }
        public string origProjectedScore { get; set; }
        public double liveRemainingProjectedScore { get; set; }
        public string nflTeam { get; set; }
    }

    public class LiveScoringPlayers
    {
        public List<LiveScorePlayer> player { get; set; }
    }

    public class LiveScoreFranchise
    {
        public string playersCurrentlyPlaying { get; set; }
        public string gameSecondsRemaining { get; set; }
        public string isHome { get; set; }
        public LiveScoringPlayers players { get; set; }
        public string playersYetToPlay { get; set; }
        public string score { get; set; }
        public string id { get; set; }
        public string owner { get; set; }
    }

    public class Matchup
    {
        public List<LiveScoreFranchise> franchise { get; set; }
    }

    public class LiveScoring
    {
        public List<Matchup> matchup { get; set; }
        public string week { get; set; }
    }

    public class LiveScoreRoot
    {
        public LiveScoring liveScoring { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }
}
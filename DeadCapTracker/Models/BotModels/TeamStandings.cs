using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class StandingsV2
    {
        public int FranchiseId { get; set; }
        public List<AnnualScoringData> TeamStandings { get; set; }
    }

    public class AnnualScoringData
    {
        public int FranchiseId { get; set; }
        public int Year { get; set; }
        public decimal PointsFor { get; set; }
        public int H2hWins { get; set; }
        public int H2hLosses { get; set; }
        public int VictoryPoints { get; set; }

    }

    public class TeamStandings
    {
        public int FranchiseId { get; set; }
        public decimal PointsFor1 { get; set; }
        public int H2hWins1 { get; set; }
        public int H2hLosses1 { get; set; }
        public int VictoryPoints1 { get; set; }
        public int AllPlayWins1 { get; set; }
        public int AllPlayLosses1 { get; set; }
        public int AllPlayTies1 { get; set; }
        public decimal PointsFor2 { get; set; }
        public int H2hWins2 { get; set; }
        public int H2hLosses2 { get; set; }
        public int VictoryPoints2 { get; set; }
        public int AllPlayWins2 { get; set; }
        public int AllPlayLosses2 { get; set; }
        public int AllPlayTies2 { get; set; }
        public decimal PointsFor3 { get; set; }
        public int H2hWins3 { get; set; }
        public int H2hLosses3 { get; set; }
        public int VictoryPoints3 { get; set; }
        public int AllPlayWins3 { get; set; }
        public int AllPlayLosses3 { get; set; }
        public int AllPlayTies3 { get; set; }
    }
}
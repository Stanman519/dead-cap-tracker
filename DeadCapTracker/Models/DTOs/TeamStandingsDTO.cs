namespace DeadCapTracker
{
    public class TeamStandingsDTO
    {
        public int FranchiseId { get; set; }
        public decimal PointsFor { get; set; }
        public int H2hWins { get; set; }
        public int H2hLosses { get; set; }
        public int VictoryPoints { get; set; }
        public int AllPlayWins { get; set; }
        public int AllPlayLosses { get; set; }
        public int AllPlayTies { get; set; }
    }
}
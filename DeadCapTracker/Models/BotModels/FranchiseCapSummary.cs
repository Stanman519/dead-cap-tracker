using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class FranchiseCapSummary
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public int AdjustedTeamCap { get; set; }
        public Dictionary<string, decimal> DeadCapData { get; set; }
        public decimal CurrentRosterSalary { get; set; }
        public decimal CurrentIRSalary { get; set; }
        public decimal CurrentTaxiSalary { get; set; }
        public decimal NextYearRosterSalary { get; set; }
    }
}
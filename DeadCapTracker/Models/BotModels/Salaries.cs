using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class SalariesRoot
    {
        public string version { get; set; }
        public string encoding { get; set; }
        public Salaries salaries { get; set; }
    }

    public class Salaries
    {
        public LeagueUnit leagueUnit { get; set; }
    }

    public class LeagueUnit
    {
        public string unit { get; set; }
        public List<PlayerSalary> player { get; set; }
    }

    public class PlayerSalary
    {
        public string contractYear { get; set; }
        public string id { get; set; }
        public string salary { get; set; }
    }
}
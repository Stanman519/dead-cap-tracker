using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class FullLeagueFranchise
    {
        public string icon { get; set; }
        public string future_draft_picks { get; set; }
        public string waiverSortOrder { get; set; }
        public string lastVisit { get; set; }
        public string email { get; set; }
        public string bbidAvailableBalance { get; set; }
        public string play_audio { get; set; }
        public string salaryCapAmount { get; set; }
        public string id { get; set; }
        public string owner_name { get; set; }
        public string mail_event { get; set; }
        public string stadium { get; set; }
        public string country { get; set; }
        public string abbrev { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public string logo { get; set; }
        public string time_zone { get; set; }
        public string use_advanced_editor { get; set; }
        public string cell2 { get; set; }
        public string cell { get; set; }
        public string wireless_carrier { get; set; }
        public string url { get; set; }
    }

    public class Franchises
    {
        public string count { get; set; }
        public List<FullLeagueFranchise> franchise { get; set; }
    }

    public class FullDetailLeague
    {
        public string url { get; set; }
        public string year { get; set; }
        public string victoryPointsEndWeek { get; set; }
        public string currentWaiverType { get; set; }
        public string playerLimitUnit { get; set; }
        public string taxiSquad { get; set; }
        public string endWeek { get; set; }
        public string maxWaiverRounds { get; set; }
        public string draft_kind { get; set; }
        public string lockout { get; set; }
        public string defaultTradeExpirationDays { get; set; }
        public string nflPoolStartWeek { get; set; }
        public string victoryPointsTie { get; set; }
        public Franchises franchises { get; set; }
        public string includeTaxiWithContractYear { get; set; }
        public string standingsSort { get; set; }
        public string draftPlayerPool { get; set; }
        public string id { get; set; }
        public string keeperType { get; set; }
        public string nflPoolType { get; set; }
        public string includeIRWithContractYear { get; set; }
        public History history { get; set; }
        public string rosterSize { get; set; }
        public string name { get; set; }
        public string draftTimer { get; set; }
        public string fantasyPoolType { get; set; }
        public RosterLimits rosterLimits { get; set; }
        public string bbidIncrement { get; set; }
        public string includeIRWithSalary { get; set; }
        public string mobileAlerts { get; set; }
        public string draftLimitHours { get; set; }
        public string victoryPointsBuckets { get; set; }
        public Starters starters { get; set; }
        public string includeTaxiWithSalary { get; set; }
        public string fantasyPoolEndWeek { get; set; }
        public string nflPoolEndWeek { get; set; }
        public string bestLineup { get; set; }
        public string precision { get; set; }
        public string victoryPointsStartWeek { get; set; }
        public string survivorPool { get; set; }
        public string lastRegularSeasonWeek { get; set; }
        public string bbidTiebreaker { get; set; }
        public string usesContractYear { get; set; }
        public string injuredReserve { get; set; }
        public string bbidConditional { get; set; }
        public string salaryCapAmount { get; set; }
        public string commish_username { get; set; }
        public string startWeek { get; set; }
        public string victoryPointsLoss { get; set; }
        public string survivorPoolStartWeek { get; set; }
        public string fantasyPoolStartWeek { get; set; }
        public string survivorPoolEndWeek { get; set; }
        public string rostersPerPlayer { get; set; }
        public string h2h { get; set; }
        public string usesSalaries { get; set; }
        public string victoryPointsWin { get; set; }
        public string bbidMinimum { get; set; }
        public string baseURL { get; set; }
        public string loadRosters { get; set; }
    }

    public class History
    {
        public List<League> league { get; set; }
    }

    public class Position
    {
        public string name { get; set; }
        public string limit { get; set; }
    }

    public class RosterLimits
    {
        public List<Position> position { get; set; }
    }

    public class Starters
    {
        public string count { get; set; }
        public List<Position> position { get; set; }
        public string iop_starters { get; set; }
    }

    public class LeagueRoot
    {
        public string version { get; set; }
        public FullDetailLeague league { get; set; }
        public string encoding { get; set; }
    }


}
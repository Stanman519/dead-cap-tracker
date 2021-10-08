using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class ScheduleFranchise
    {
        public string isHome { get; set; }
        public string score { get; set; }
        public string id { get; set; }
        public string result { get; set; }
        public string spread { get; set; }
    }

    public class ScheduleMatchup
    {
        public List<ScheduleFranchise> franchise { get; set; }
    }

    public class WeeklySchedule
    {
        public List<ScheduleMatchup> matchup { get; set; }
        public string week { get; set; }
    }

    public class Schedule
    {
        public List<WeeklySchedule> weeklySchedule { get; set; }
    }

    public class ScheduleRoot
    {
        public string version { get; set; }
        public Schedule schedule { get; set; }
        public string encoding { get; set; }
    }
}
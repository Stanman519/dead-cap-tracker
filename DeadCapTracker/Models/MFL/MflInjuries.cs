using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class Injury
    {
        public string exp_return { get; set; }
        public string status { get; set; }
        public string id { get; set; }
        public string details { get; set; }
    }

    public class Injuries
    {
        public string timestamp { get; set; }
        public string week { get; set; }
        public List<Injury> injury { get; set; }
    }

    public class InjuriesRoot
    {
        public string version { get; set; }
        public Injuries injuries { get; set; }
        public string encoding { get; set; }
    }

}
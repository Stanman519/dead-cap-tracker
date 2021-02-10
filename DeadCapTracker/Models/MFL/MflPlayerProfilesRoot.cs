using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class MflPlayerProfilesRoot
    {
        public MflPlayerProfileParent playerProfiles { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }

    public class MflPlayerProfileParent
    {
        public List<MflPlayerProfile> playerProfile { get; set; }
    }

    public class MflPlayerProfile
    {
        public string Name { get; set; }
        public MflPlayerDetails player { get; set; }
        public string Id { get; set; }

    }
    

    public class MflPlayerDetails
    {
        public string dob { get; set; }
        public string adp { get; set; }
        public string weight { get; set; }
        public string id { get; set; }
        public string height { get; set; }
        public string age { get; set; }

    }
}
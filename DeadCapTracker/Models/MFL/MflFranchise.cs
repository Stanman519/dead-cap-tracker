

#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeadCapTracker.Models.MFL
{
    
    public class LeagueInfo
    {
        public string Version { get; set; }
        public League League { get; set; }
        public string Encoding { get; set; }
    }
    public class League
    {
        public FranchiseParent Franchises { get; set; }
    }
    
    public class FranchiseParent
    {
        public List<MflFranchise> Franchise { get; set; }
    }
    [JsonObject]
    public class MflFranchise
    {
        public string Icon { get; set; }
        public string BBidAvailableBalance { get; set; }
        public string Id { get; set; }
        
        [JsonProperty(PropertyName = "owner_name")]
        public string? Owner_name { get; set; }
        public string Abbrev { get; set; }
        public string Name { get; set; }

        
    }
}


using Newtonsoft.Json;

namespace DeadCapTracker.Models.MFL
{
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
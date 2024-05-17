using Newtonsoft.Json;
using System;

namespace DeadCapTracker.Models.DTOs
{
    public class BidDTO
    {
        [JsonProperty("bidId")]
        public int BidId { set; get; }
        [JsonProperty("bidLength")]
        public int BidLength { set; get; }
        [JsonProperty("bidSalary")]
        public int BidSalary { set; get; }
        [JsonProperty("ownername")]
        public string Ownername { set; get; }
        [JsonProperty("expires")]
        public DateTime Expires { set; get; }
        [JsonProperty("lotId")]
        public int? LotId { set; get; }
        [JsonProperty("ownerId")]
        public int OwnerId { get; set; }
        [JsonProperty("leagueId")]
        public int LeagueId { get; set; }
        [JsonProperty("player")]
        public PlayerDTO Player { get; set; }
    }

    public class PlayerDTO
    {
        public string FirstName { set; get; }
        public string LastName { set; get; }
        public string? FullName { get; set; }
        public string? Team { get; set; }
        public int? Age { get; set; }
        public string? Headshot { get; set; }
        public string? ActionShot { get; set; }
        public int? Salary { set; get; }
        public int? Length { set; get; }
        public string? Position { set; get; }
        public int MflId { set; get; }
        public int? ContractValue { set; get; }
    }
}

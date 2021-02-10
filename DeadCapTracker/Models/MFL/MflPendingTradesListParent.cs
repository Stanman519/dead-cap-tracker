using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeadCapTracker.Models.MFL
{
    public class MflPendingTradesListParent
    {
        [JsonPropertyName("pendingTrade")]
        public List<MflPendingTrade> PendingTrade { get; set; }
        // [JsonPropertyName("pendingTrade")]
        // public MflPendingTrade pendingTrade { get; set; }
    }
}
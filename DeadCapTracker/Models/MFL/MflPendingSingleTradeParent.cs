using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeadCapTracker.Models.MFL
{
    public class MflPendingSingleTradeParent
    {
        [JsonPropertyName("pendingTrade")]
        public MflPendingTrade PendingTrade { get; set; }
    }
}
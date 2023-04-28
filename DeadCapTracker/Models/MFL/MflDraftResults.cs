using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeadCapTracker.Models.MFL
{
    public class DraftPickWithSlotValue : RookieDraftPick
    {
        public int Salary { get; set; }
        public int Length { get; set; }
    }
    public class RookieDraftPick
    {
        [JsonPropertyName("pick")]
        public string Pick { get; set; }

        [JsonPropertyName("franchise")]
        public string Franchise { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("player")]
        public string Player { get; set; }

        [JsonPropertyName("round")]
        public string Round { get; set; }

        [JsonPropertyName("comments")]
        public string Comments { get; set; }
    }

    public class DraftResults
    {
        [JsonPropertyName("draftUnit")]
        public DraftUnit DraftUnit { get; set; }
    }

    public class DraftUnit
    {
        [JsonPropertyName("draftPick")]
        public List<RookieDraftPick> DraftPick { get; set; }

        [JsonPropertyName("round1DraftOrder")]
        public string Round1DraftOrder { get; set; }

        [JsonPropertyName("draftType")]
        public string DraftType { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        [JsonPropertyName("static_url")]
        public string StaticUrl { get; set; }
    }

    public class MflDraftResultRoot
    {
        [JsonPropertyName("encoding")]
        public string Encoding { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("draftResults")]
        public DraftResults DraftResults { get; set; }
    }


}

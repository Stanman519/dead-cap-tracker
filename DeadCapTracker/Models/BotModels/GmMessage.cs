namespace DeadCapTracker.Models.BotModels
{
    public class GmMessage
    {
        public string avatar_url { get; set; }
        public int created_at { get; set; }
        public string group_id { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string sender_id { get; set; }
        public string sender_type { get; set; }
        public string source_guid { get; set; }
        public bool system { get; set; }
        public string text { get; set; }
        public string user_id { get; set; }
    }
}
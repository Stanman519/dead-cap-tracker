namespace DeadCapTracker.Models.MFL
{
    public class MflPendingTrade
    {
        public string trade_id { get; set; }
        public string offeredTo { get; set; }
        public string offeringTeam { get; set; }
        public string comments { get; set; }
        public string description { get; set; }
        public string will_receive { get; set; }
        public string timestamp { get; set; }
        public string will_give_up { get; set; }
        public string expires { get; set; }
    }
}
namespace DeadCapTracker.Models.MFL
{
    public class MflPendingTradesListRoot
    {
        public string Version { get; set; }
        public MflPendingTradesListParent pendingTrades { get; set; }
        public string Encoding { get; set; }
    }
}
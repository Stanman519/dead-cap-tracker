namespace DeadCapTracker.Models.MFL
{
    public class MflPendingSingleTradeRoot
    {
        public string Version { get; set; }
        public MflPendingSingleTradeParent pendingTrades { get; set; }
        public string Encoding { get; set; }
    }
}
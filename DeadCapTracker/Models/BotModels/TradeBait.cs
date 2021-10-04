using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class TradeBait
    {
        public string timestamp { get; set; }
        public string franchise_id { get; set; }
        public string willGiveUp { get; set; }
        public string inExchangeFor { get; set; }
    }

    public class TradeBaitsParent
    {
        public string version { get; set; }
        public string encoding { get; set; }
        public TradeBaitsMulti tradeBaits { get; set; }
    }

    public class TradeBaitsMulti
    {
        public List<TradeBait> tradeBait { get; set; }
    }

    public class TradeBaitParent
    {
        public string version { get; set; }
        public string encoding { get; set; }
        public TradeBaitsSingle tradeBaits { get; set; }
    }

    public class TradeBaitsSingle
    {
        public TradeBait tradeBait { get; set; }
    }
}
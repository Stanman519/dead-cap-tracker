using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class MFLResponseParent
    {
        public string version { get; set; }
        public string encoding { get; set; }
    }

    public class TradeBaitParent : MFLResponseParent
    {
        public TradeBaitSingle TradeBaits { get; set; }
    }
    public class TradeBaitsParent : MFLResponseParent
    {
        public TradeBaitsMulti tradeBaits { get; set; }
    }
    
    public class TradeBaitsMulti 
    {
        public List<TradeBait> tradeBait { get; set; }
    }

    public class TradeBaitSingle
    {
        public TradeBait tradeBait { get; set; }
    }
    
    public class TradeBait
    {
        public string timestamp { get; set; }
        public string franchise_id { get; set; }
        public string willGiveUp { get; set; }
        public string inExchangeFor { get; set; }
    }
    


    

}
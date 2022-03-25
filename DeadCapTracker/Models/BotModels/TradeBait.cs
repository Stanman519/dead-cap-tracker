using System.Collections.Generic;
using DeadCapTracker.Services;
using Newtonsoft.Json;

namespace DeadCapTracker.Models.BotModels
{
    public class MFLResponseParent
    {
        public string version { get; set; }
        public string encoding { get; set; }
    }
    
    public class TradeBaitsParent : MFLResponseParent
    {
        public TradeBaitsMulti tradeBaits { get; set; }
    }

    public class TradeBaitsMulti
    {
        [JsonConverter(typeof(SingleOrArrayConverter<TradeBait>))]
        public List<TradeBait> tradeBait { get; set; } = new List<TradeBait>();
    }
    public class TradeBait
    {
        public string timestamp { get; set; }
        public string franchise_id { get; set; }
        public string willGiveUp { get; set; }
        public string inExchangeFor { get; set; }
    }

}
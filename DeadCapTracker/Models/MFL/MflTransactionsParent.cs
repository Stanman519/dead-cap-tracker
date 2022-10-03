using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Services;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeadCapTracker.Models.MFL
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class MflTransactionsParent
    {
        public string version { get; set; }
        public MflTransactionsList transactions { get; set; }
        public string encoding { get; set; }
    }

    public class MflTransaction
    {
        public string transaction { get; set; }
        public string timestamp { get; set; }
        public string franchise { get; set; }
        public string type { get; set; }
        public string action { get; set; }
        public string original_timestamp { get; set; }
        public string activated { get; set; }
        public string deactivated { get; set; }
        public string franchise2 { get; set; }
        public string franchise2_gave_up { get; set; }
        public string expires { get; set; }
        public string franchise1_gave_up { get; set; }
    }

    public class MflTransactionsList
    {
        [JsonConverter(typeof(SingleOrArrayConverter<MflTransaction>))]
        public List<MflTransaction> transaction { get; set; }
    }


}

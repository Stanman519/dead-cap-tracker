using System.Collections.Generic;
using DeadCapTracker.Models.BotModels;

namespace DeadCapTracker.Models.MFL
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class DraftPick
    {
        public string pick { get; set; }
        public string description { get; set; }
    }

    public class FutureYearDraftPicks
    {
        public List<DraftPick> draftPick { get; set; }
    }
    

    public class Players
    {
        public List<Player> player { get; set; }
    }

    public class MflAssetsFranchise
    {
        public FutureYearDraftPicks futureYearDraftPicks { get; set; }
        public Players players { get; set; }
        public string id { get; set; }
    }

    public class Assets
    {
        public List<MflAssetsFranchise> franchise { get; set; }
    }

    public class MflAssetsRoot
    {
        public Assets assets { get; set; }
        public string version { get; set; }
        public string encoding { get; set; }
    }


}
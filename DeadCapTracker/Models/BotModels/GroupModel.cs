using System.Collections.Generic;

namespace DeadCapTracker.Models.BotModels
{
    public class GroupModel
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<MemberModel> members { get; set; }
    }
}
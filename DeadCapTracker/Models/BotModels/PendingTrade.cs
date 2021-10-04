using System;

namespace DeadCapTracker.Models.BotModels
{
    public class PendingTrade
    {        
        public int tradeId { get; set; }
        public int offeredTo { get; set; }
        public int offeringTeam { get; set; }
        public string comments { get; set; }
        public string description { get; set; }
        public string willReceive { get; set; }
        public string willGiveUp { get; set; }
        public DateTime timeStamp { get; set; }
        public DateTime expires { get; set; }
        
    }
}
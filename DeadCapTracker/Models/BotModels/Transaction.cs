using System;

#nullable disable

namespace DeadCapTracker.Models.BotModels
{
    public partial class Transaction
    {
        public DateTime? Timestamp { get; set; }
        public int Transactionid { get; set; }
        public int Franchiseid { get; set; }
        public int? Salary { get; set; }
        public decimal Amount { get; set; }
        public string Playername { get; set; }
        public string Position { get; set; }
        public string Team { get; set; }
        public int Years { get; set; }
        public int? Yearoftransaction { get; set; }
    }
}

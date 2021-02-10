using System;

namespace DeadCapTracker
{
    public class TransactionDTO
    {
        public DateTime Timestamp { get; set; }
        public int TransactionId { set; get; }
        public int FranchiseId { get; set; }
        public int Salary { get; set; }
        public double Amount { get; set; }
        public string PlayerName { get; set; }
        public string Position { get; set; }
        public string Team { get; set; }
        public int Years { get; set; }
        public int YearOfTransaction { get; set; }
    }
}
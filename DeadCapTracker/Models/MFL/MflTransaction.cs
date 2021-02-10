using System.Text.Json.Serialization;

namespace DeadCapTracker.Models.MFL
{
    public class MflTransaction
    {
        
        public string Timestamp { get; set; }
        public string Id { get; set; }
        public string Franchise_Id { get; set; }
        public string Amount { get; set; }
        public string Description { get; set; } 
        public int YearOfTransaction { get; set; }
    }
}
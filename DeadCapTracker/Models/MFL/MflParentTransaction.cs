using System.Collections.Generic;

namespace DeadCapTracker.Models.MFL
{
    public class MflSalaryAdjParent
    {
        public string version { get; set; }
        public ChildTransaction salaryAdjustments { get; set; }
        public string encoding { set; get; }
    }
    public class ChildTransaction
    {
        public List<MflSalaryAdjustment> salaryAdjustment { get; set; }
    }
    public class MflSalaryAdjustment
    {
        
        public string Timestamp { get; set; }
        public string Id { get; set; }
        public string Franchise_Id { get; set; }
        public string Amount { get; set; }
        public string Description { get; set; } 
        public int YearOfTransaction { get; set; }
    }
}
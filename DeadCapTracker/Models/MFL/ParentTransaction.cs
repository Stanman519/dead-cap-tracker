namespace DeadCapTracker.Models.MFL
{
    public class ParentTransaction
    {
        public string version { get; set; }
        public ChildTransaction salaryAdjustments { get; set; }
        public string encoding { set; get; }
    }
}
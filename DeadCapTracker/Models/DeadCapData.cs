using System.Collections.Generic;

namespace DeadCapTracker.Models
{
    public class DeadCapData
    {
        public string Team { get; set; }
        public List<decimal> Amount { get; set; }
        public int FranchiseId { get; set; }

        public DeadCapData(int id, string name)
        {
            Amount = new List<decimal> {0};
            FranchiseId = id;
            Team = name;
        }

        public void AddPenalties(int yearOfTransaction, decimal amount, int numOfYears)
        {
            var indicesRequired = yearOfTransaction + numOfYears - 2020;
            if (Amount.Count < indicesRequired)
            {
                var addsNeeded = indicesRequired - Amount.Count;
                for (int x = 0; x < addsNeeded; x++)
                {
                    Amount.Add(0);
                }
            }
            
            for (int x = 0; x < numOfYears; x++)
            {
                Amount[(yearOfTransaction + x) - 2020] += amount;
            }
        }
    }
    
}
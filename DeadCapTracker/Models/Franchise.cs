using System;
using System.Collections.Generic;

#nullable disable

namespace DeadCapTracker.Models
{
    public partial class Franchise
    {
        public int Franchiseid { get; set; }
        public string Icon { get; set; }
        public double? Bbidavailablebalance { get; set; }
        public string Ownername { get; set; }
        public string Abbrev { get; set; }
        public string Teamname { get; set; }
    }
}

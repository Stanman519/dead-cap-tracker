using System.Collections.Generic;

namespace DeadCapTracker.Services
{
    public static class Utils
    {
        public const int ThisYear = 2022;
        public static Dictionary<int, string> owners = new Dictionary<int, string>()
        {
            {1, "Ryan"},
            {2, "Tyler"},
            {3, "Caleb"},
            {4, "Trent"},
            {5, "Taylor"},
            {6, "Logan"},
            {7, "Cory"},
            {8, "Jeremi"},
            {9, "Levi"},
            {10, "Aaron"},
            {11, "Juan"},
            {12, "Drew"}
        };
        
        public static Dictionary<int, string> memberIds = new Dictionary<int, string>()
        {
            {1, "8206212"},
            {2, "36741"},
            {3, "8206213"},
            {4, "2513723"},
            {5, "482066"},
            {6, "34951757"},
            {7, "51268339"},
            {8, "36739"},
            {9, "30472260"},
            {10, "11902182"},
            {11, "36740"},
            {12, "63384702"}
        };
    }
}
using System;
using System.Collections.Generic;

namespace DeadCapTracker.Services
{
    public static class Utils
    {
        /*public const int ThisYear = 2024;*/
        public static Dictionary<int, string> ApiKeys = new Dictionary<int, string>() {
            {13894, "ahBv1sGSvuWqx0CmOVrJaDYeFbox"},
            {26548, "ahBv1sGSvuWqx0CmOl/EZToeFbox"}
         };
        public const string LongTermPlayerHack = "14777"; // Joe Burrow, used to add to mfl player lookups so you always get array rather than obj
        public const int TaxiSquadSlots = 5;
        public const double TaxiSquadCapHit = 0.2;
        public const double InjuredReserveCapHit = 0.5;
        public const double CutCapHit = 0.5;
        public static List<Tuple<string, int>> GmGroupToMflLeague = new List<Tuple<string, int>>
        {
            {"59795205", 13894 },
            {"101420221", 26548 }
        };
        public static Dictionary<int, string> leagueBotDict = new Dictionary<int, string>{
            { 13894, "6cac06cf66a381ec605c8fff25" },
            { 26548, "d3c557c0ccf5d8b10e5f0ef195"}
        };
        public static Dictionary<int, Dictionary<int, string>> owners = new Dictionary<int, Dictionary<int, string>>()
        {
             {13894, new Dictionary<int, string>()
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
                }
            },
            {26548, new Dictionary<int, string>()
                {
                    {1, "Ryan"},
                    {2, "Tyler"},
                    {3, "Logan"},
                    {4, "Trent"},
                    {5, "Cory"},
                    {6, "Aaron"},
                    {7, "Drew"},
                    {8, "Mike"},
                    {9, "Taylor"},
                    {10, "Eduardo"},
                    {11, "Marlz & Rob"},
                    {12, "Kevin"}
                }
            }
        };
        
        public static Dictionary<int, Dictionary<int, string>> memberIds = new Dictionary<int, Dictionary<int, string>>()
        {
            {13894, new Dictionary<int, string>()
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
                }
            },
            {26548, new Dictionary<int, string>()
                {
                    {1, "8206212"},
                    {2, "36741"},
                    {3, "34951757"},
                    {4, "2513723"},
                    {5, "51268339"},
                    {6, "11902182"},
                    {7, "63384702"},
                    {8, "63384793"},
                    {9, "482066"},
                    {10, "2513726"},
                    {11, "21381949,14091752"},
                    {12, "73982813"}
                }
            }

        };
        public static Dictionary<int, int> draftPicks = new Dictionary<int, int>
        {
            {1, 30},
            {2, 28},
            {3, 26},
            {4, 24},
            {5, 22},
            {6, 22},
            {7, 22},
            {8, 22},
            {9, 20},
            {10, 20},
            {11, 20},
            {12, 20},
            {13, 18},
            {14, 18},
            {15, 18},
            {16, 18},
            {17, 16},
            {18, 16},
            {19, 16},
            {20, 16},
            {21, 14},
            {22, 14},
            {23, 14},
            {24, 14},
            {25, 12},
            {26, 12},
            {27, 12},
            {28, 12},
            {29, 10},
            {30, 10},
            {31, 10},
            {32, 10},
            {33, 8},
            {34, 8},
            {35, 8},
            {36, 8},
            {37,  6}
        };

        }
    public static class TupleListExtensions
    {
        public static void Add<T1, T2>(this IList<Tuple<T1, T2>> list,
                T1 item1, T2 item2)
        {
            list.Add(Tuple.Create(item1, item2));
        }
    }
}
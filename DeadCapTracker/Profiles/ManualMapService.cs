using System;
using System.Collections.Generic;
using System.Linq;
using DeadCapTracker.Models;
using DeadCapTracker.Models.MFL;

namespace DeadCapTracker.Profiles
{
    public class ManualMapService
    {
        public List<TeamStandings> MapOneYearStandings(List<MflFranchiseStandings> one)
        {
            var t = new List<TeamStandings>();
            foreach (var o in one)
            {
                var team = new TeamStandings
                {
                    FranchiseId = Int32.Parse(o.id),
                    H2hLosses1 = Int32.Parse(o.h2hl),
                    H2hWins1 = Int32.Parse(o.h2hw),
                    // VictoryPoints1 = Int32.Parse(o.vp),
                    // AllPlayLosses1 = Int32.Parse(o.all_play_l),
                    // AllPlayTies1 = Int32.Parse(o.all_play_t),
                    // AllPlayWins1 = Int32.Parse(o.all_play_w),
                    PointsFor1 = Decimal.Parse(o.pf)
                };
                t.Add(team);
            }
        return t;
        }
        public List<TeamStandings> MapTwoYearStandings(List<MflFranchiseStandings> one, List<MflFranchiseStandings> two)
        {
            var t = new List<TeamStandings>();
            foreach (var o in one)
            {
                var yearTwo = two.First(_ => Int32.Parse((string) _.id) == Int32.Parse(o.id));
                var team = new TeamStandings
                {
                    FranchiseId = Int32.Parse(o.id),
                    H2hLosses1 = Int32.Parse(o.h2hl),
                    H2hWins1 = Int32.Parse(o.h2hw),
                    VictoryPoints1 = Int32.Parse(o.vp),
                    // AllPlayLosses1 = Int32.Parse(o.all_play_l),
                    // AllPlayTies1 = Int32.Parse(o.all_play_t),
                    // AllPlayWins1 = Int32.Parse(o.all_play_w),
                    PointsFor1 = Decimal.Parse(o.pf), 
                    H2hLosses2 = Int32.Parse(yearTwo.h2hl),
                    H2hWins2 = Int32.Parse(yearTwo.h2hw) + (Int32.Parse(yearTwo.vp) - (Int32.Parse(yearTwo.h2hw) * 2)),
                    VictoryPoints2 = Int32.Parse(yearTwo.vp),
                    // AllPlayLosses2 = Int32.Parse(yearTwo.all_play_l),
                    // AllPlayTies2 = Int32.Parse(yearTwo.all_play_t),
                    // AllPlayWins2 = Int32.Parse(yearTwo.all_play_w),
                    PointsFor2 = Decimal.Parse(yearTwo.pf)
                };
                t.Add(team);
            }

            return t;
        }
        public List<TeamStandings> MapThreeYearStandings(List<MflFranchiseStandings> one, List<MflFranchiseStandings> two,
            List<MflFranchiseStandings> three)
        {
            var t = new List<TeamStandings>();
            foreach (var o in one)
            {
                var yearTwo = two.Where(_ => Int32.Parse(_.id) == Int32.Parse(o.id)).First();
                var yearThree = three.Where(_ => Int32.Parse(_.id) == Int32.Parse(o.id)).First();
                var team = new TeamStandings
                {
                    FranchiseId = Int32.Parse(o.id),
                    H2hLosses1 = Int32.Parse(o.h2hl),
                    H2hWins1 = Int32.Parse(o.h2hw),
                    VictoryPoints1 = Int32.Parse(o.vp),
                    // AllPlayLosses1 = Int32.Parse(o.all_play_l),
                    // AllPlayTies1 = Int32.Parse(o.all_play_t),
                    // AllPlayWins1 = Int32.Parse(o.all_play_w),
                    PointsFor1 = Decimal.Parse(o.pf), 
                    H2hLosses2 = Int32.Parse(yearTwo.h2hl),
                    H2hWins2 = Int32.Parse(yearTwo.h2hw) + (Int32.Parse(yearTwo.vp) - (Int32.Parse(yearTwo.h2hw) * 2)),
                    VictoryPoints2 = Int32.Parse(yearTwo.vp),
                    // AllPlayLosses2 = Int32.Parse(yearTwo.all_play_l),
                    // AllPlayTies2 = Int32.Parse(yearTwo.all_play_t),
                    // AllPlayWins2 = Int32.Parse(yearTwo.all_play_w),
                    PointsFor2 = Decimal.Parse(yearTwo.pf),
                    H2hLosses3 = Int32.Parse(yearThree.h2hl),
                    H2hWins3 = Int32.Parse(yearThree.h2hw) + (Int32.Parse(yearThree.vp) - (Int32.Parse(yearThree.h2hw) * 2)),
                    VictoryPoints3 = Int32.Parse(yearThree.vp),
                    // AllPlayLosses3 = Int32.Parse(yearThree.all_play_l),
                    // AllPlayTies3 = Int32.Parse(yearThree.all_play_t),
                    // AllPlayWins3 = Int32.Parse(yearThree.all_play_w),
                    PointsFor3 = Decimal.Parse(yearThree.pf)
                };
                t.Add(team);
            }

            return t;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Repositories;
using Microsoft.Extensions.Logging;

namespace DeadCapTracker.Services
{
    public interface IGroupMeRequestService
    {
        public Task<List<AnnualScoringData>> PostStandingsToGroup(string botId, int leagueId, int year);
        public Task<List<PendingTradeDTO>> PostTradeOffersToGroup(string botId, int leagueId, int year);
        public Task PostTradeRumor(string botId, int leagueId);
        public Task PostCompletedTradeToGroup(string botId, int leagueId);
        public Task<string> FindAndPostContract(string botId, int leagueId, int year, string nameSearch);
        Task<string> FindAndPostLiveScores(string botId, int leagueId);
        Task CheckLineupsForHoles(string botId, int leagueId);
        Task PostHelpMessage(string botId);
        Task PostCapSpace(string botId, int leagueId);
        Task PostDraftProjections(string botId, int leagueId, int year);
        Task StrayTag(string botId);
        Task PostTopUpcomingFreeAgents(string botId, int leagueId, string positionRequested, int year);
        Task PostFranchiseTagAmounts(string botId, int leagueId);
        Task PostFutureDeadCap(string botId);
        Task BotPost(string botId, string post, bool isError = false);
        Task PostDraftBudgets(string botId, int leagueId);
    }
    
    public class GroupMeRequestService : IGroupMeRequestService
    {
        private readonly IMflTranslationService _mflTranslationService;
        private readonly IDataSetHelperService _dataHelper;
        private IGroupMePostRepo _gm;
        private readonly ILeagueService _leagueService;
        private readonly IRumorService _rumor;
        private readonly IInsultApi _insult;
        private readonly ILogger<GroupMeRequestService> _logger;
        private static Dictionary<int, Dictionary<int, string>> _owners;
        private static Dictionary<int, Dictionary<int, string>> _memberIds;

        
        public GroupMeRequestService(IMflTranslationService mflTranslationService, 
            IDataSetHelperService dataHelper, 
            IGroupMePostRepo gm, 
            ILeagueService leagueService, 
            IRumorService rumor, 
            IInsultApi insult,
            ILogger<GroupMeRequestService> logger)
        {
            _mflTranslationService = mflTranslationService;
            _dataHelper = dataHelper;
            _gm = gm;
            _leagueService = leagueService;
            _rumor = rumor;
            _insult = insult;
            _logger = logger;
            _owners = Utils.owners;
            _memberIds = Utils.memberIds;

        }

        public async Task<List<AnnualScoringData>> PostStandingsToGroup(string botId, int leagueId, int year)
        {

            var standingsData = await _leagueService.GetStandingsV2(leagueId, year);

            var standings = standingsData.SelectMany(s => s.TeamStandings)
                .Where(s => s.Year == year)
                .OrderByDescending(s => s.VictoryPoints)
                .ThenByDescending(s => s.H2hWins)
                .ThenByDescending(s => s.PointsFor)
                .ToList();
            var strForBot = "STANDINGS\n";
            var tytString = "Tri-Year Trophy Presented by Taco Bell\nTOP 5\n";
            standings.ForEach(s =>
            {
                strForBot = $"{strForBot}{_owners[s.FranchiseId]}  ({s.VictoryPoints} VP)  {s.H2hWins}-{s.H2hLosses}    {s.PointsFor} pts\n";
            });
            // TODO: add guard to check if this is year one of cycle - not worth posting if so
            var tytScores = standingsData.Select(t => new TYTScore
            {
                Owner = _owners[leagueId][t.FranchiseId],
                Score = t.TeamStandings.Sum(s => s.PointsFor) + t.TeamStandings.Select(s => s.H2hWins * 10).Sum()
            }).OrderByDescending(t => t.Score)
                .Take(5)
                .ToList();

            tytScores.ForEach(s =>
            {
                tytString = $"{tytString}{s.Owner} - {s.Score}\n";
            });
            await _gm.BotPost(botId, strForBot);
            await _gm.BotPost(botId, tytString);
            return standings;
        }

        // I dont know what i was doing here?
/*        public int GetAdjustedWins(int h2hWins, int vp)
        {
            return h2hWins + vp - (h2hWins * 2);
        }*/

        public async Task<List<PendingTradeDTO>> PostTradeOffersToGroup(string botId, int leagueId, int year)
        {
            var tenMinDuration = new TimeSpan(0, 0, 10, 0);
            var trades = await _leagueService.FindPendingTrades(leagueId, year);
            var memberList = (await _gm.GetMemberIds()).response.members;

            string strForBot = "";
           
            if (trades.Count > 0)
            {
                trades.ForEach(async t =>
                {
                    var timeDifference = t.timeStamp.TimeOfDay - DateTime.Now.AddMinutes(-11).TimeOfDay;
                    if (timeDifference.Ticks > 0 && timeDifference < tenMinDuration)
                    {
                        // get member id, then lookup their name;
                        var tagName = memberList.Find(m => m.user_id == _memberIds[leagueId][t.offeredTo]);
                        var tagString = $"@{tagName.nickname}";
                        strForBot = ", you have a pending trade offer!";
                        await _gm.BotPostWithTag(botId, strForBot, tagString, tagName.user_id);
                    }
                });
            }
            return trades;
        }

        public async Task PostCompletedTradeToGroup(string botId, int leagueId)
        {
            DateTime tenMinAgo = DateTime.Now.AddMinutes(-11);

            var tradeInfoList = await _mflTranslationService.GetCompletedTrades(leagueId);
            //TODO: CHECK FOR NULL?? it would mean it could not serialize it as either type
            
            foreach (var trade in tradeInfoList)
            {
                if (trade != null && !string.IsNullOrEmpty(trade.franchise2))
                {
                    var tradeTime = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(trade.timestamp));
                    // check if trade was not in the last 10 minutes to bail early
                    if (tradeTime <= tenMinAgo) continue;
                    var strForBot = await _rumor.GetCompletedTradeString(leagueId, trade);
                    await _gm.BotPost(botId, strForBot);
                }
            }
        }

        public async Task PostTradeRumor(string botId, int leagueId)
        {
            // TODO: make the rumor service a string builder service.

            var baitList = await _mflTranslationService.GetNewTradeBait(leagueId);

            foreach (var post in baitList)
            {
                if (post?.timestamp != null)
                {
                    var strForBot = "";
                    var postDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(post.timestamp));
                    if (postDate > DateTime.Now.AddMinutes(-11))
                    {
                        strForBot = await _rumor.GetTradeBaitString(leagueId, post);
                        await _gm.BotPost(botId, strForBot);
                    }
                }
            }
        }

        public async Task<string> FindAndPostContract(string botId, int leagueId, int year, string nameSearch)
        {
            if (nameSearch.EndsWith(" ")) nameSearch = nameSearch.Trim();
            var hits = await _mflTranslationService.GetRosteredPlayersByName(leagueId, year, nameSearch);
            // bot post those names and contracts
            var stringForBot = "";
            if (hits == null) stringForBot = "I dunno, man. Mfl is busted.";
            else if (!hits.Any())
                stringForBot = "I couldn't find any players by that name with contracts.";
            else
            {
                hits.ForEach(p =>
                {
                    stringForBot = $"{stringForBot}{p.name} - ${p.salary}/{p.contractYear} ({p.owner})\n";
                });
            }
            await _gm.BotPost(botId, stringForBot);
            return stringForBot;
        }

        public async Task CheckLineupsForHoles(string botId, int leagueId)
        {
            var thisWeek = await _mflTranslationService.GetThisLeagueWeek(leagueId);

            // task when all get injuries and byes get scores with that week num
            var scoresTask = _mflTranslationService.GetLiveScoresForFranchises(leagueId, thisWeek);
            var byesTask = _mflTranslationService.GetByesThisWeek(thisWeek);
            var injuriesTask = _mflTranslationService.GetInjurredPlayerIdsThisWeek(thisWeek);
            var allPlayersTask = _mflTranslationService.GetAllRelevantPlayers(leagueId);
            var groupTask = _gm.GetMemberIds();

            try
            {
                await Task.WhenAll(scoresTask, injuriesTask, byesTask, allPlayersTask, groupTask);
            }
            catch (Exception)
            {
                await _gm.BotPost(botId, "I don't know MFL is busted.");
                return;
            }
            
            var memberList = groupTask.Result.response.members;
            // go through starters for each team. make sure theres no OUT or BYE (could also check for other messages)
            
            var onlyStarters = scoresTask.Result.Select(tm => new LiveScoreFranchise
            {
                id = tm.id,
                players = new LiveScoringPlayers
                {
                    player = tm.players.player.Where(p => p.status.ToLower() == "starter").ToList()
                }
            }).ToList();
            var brokenTeams = new List<string>();

            foreach (var franch in onlyStarters)
            {
                var botStr = ", your lineup is invalid";
                foreach (var player in franch.players.player)
                {
                    player.nflTeam = allPlayersTask.Result.FirstOrDefault(allPlayer => allPlayer.id == player.id)?.team;
                    var hasBye = byesTask.Result.Contains(player.nflTeam);
                    var isOut = injuriesTask.Result.Contains(player.id);
                    if (!hasBye && !isOut) continue;
                    if (brokenTeams.Contains(franch.id)) continue;
                    var tagName = memberList.Find(m => m.user_id == _memberIds[leagueId][Int32.Parse(franch.id)]);
                    var tagString = $"@{tagName?.nickname}";
                    await _gm.BotPostWithTag(botId, botStr, tagString, tagName?.user_id ?? "");
                    brokenTeams.Add(franch.id);
                }
            }
            if (!brokenTeams.Any()) await _gm.BotPost(botId, "Lineups are all straight, mate.");
        }

        public async Task PostCapSpace(string botId, int leagueId)
        {
            var botStr = "Current Cap Space (Next Year)\n";
            var _thisYear = DateTime.Now.Year;
            //get total salaries this season + adjustments, subtract from team budget
            var salaryAdjustmentsTask = _mflTranslationService.GetSalaryAdjustments(leagueId, _thisYear);
            var salariesTask = _mflTranslationService.GetFranchiseSalaries(leagueId);
            var leagueTask = _mflTranslationService.GetTeamAdjustedSalaryCaps(leagueId);
            try
            {
                await Task.WhenAll(salariesTask, leagueTask, salaryAdjustmentsTask);
            }
            catch (Exception e)
            {
                await _gm.BotPost(botId, "I'm having trouble reaching MFL. Try again later.");
            }


            var thisSznAdj = salaryAdjustmentsTask.Result ?? new List<MflSalaryAdjustment>();
            var adjustments = await _leagueService.GetDeadCapData();
            // add up salaries for this year - but dont forget to * .5  and .4 for taxi and IR
            
            var orderedSummaries = _dataHelper.CreateFranchiseCapSummaries(salariesTask.Result, adjustments);
            
            orderedSummaries.ForEach(tm =>
            {
                var teamAdj = thisSznAdj.Where(adj => Int32.Parse(adj.Franchise_Id) == tm.Id)
                    .Select(adj => Decimal.TryParse(adj.Amount, out var x) ? x : 0).Sum();
                botStr += $"{_owners[leagueId][tm.Id]}: " +
                          $"${leagueTask.Result.First(_ => _.Id == tm.Id).SalaryCapAmount - (tm.CurrentRosterSalary + tm.CurrentTaxiSalary + tm.CurrentIRSalary) - teamAdj} " +
                          $"(${500 - (tm.NextYearRosterSalary + (tm.DeadCapData.ContainsKey((_thisYear + 1).ToString()) ? tm.DeadCapData[(_thisYear + 1).ToString()] : 0))})\n";
            });
            await _gm.BotPost(botId, botStr);
        }

        public async Task<string> FindAndPostLiveScores(string botId, int leagueId)
        {
            try
            {
                var thisWeek = await _mflTranslationService.GetThisLeagueWeek(leagueId);
                var matchupScoresTask = _mflTranslationService.GetLiveScoresForMatchups(leagueId, thisWeek);
                var scoreProjectionsTask = _mflTranslationService.GetProjections(leagueId,thisWeek);

                await Task.WhenAll(matchupScoresTask, scoreProjectionsTask);

                var matchups = matchupScoresTask.Result;
                var projections = scoreProjectionsTask.Result;

                var botText = "Live Scores (Live Projections)\n";

                foreach (var matchup in matchups)
                {
                    _owners[leagueId].TryGetValue(int.Parse(matchup.franchise.First().id), out var team1);
                    _owners[leagueId].TryGetValue(int.Parse(matchup.franchise.Last().id), out var team2);

                    if (!double.TryParse(matchup.franchise.First().score, out var team1Score) ||
                        !double.TryParse(matchup.franchise.Last().score, out var team2Score))
                    {
                        continue;
                    }

                    var team1Starters = matchup.franchise.First().players.player.Where(p => p.status == "starter").ToList();
                    var team2Starters = matchup.franchise.Last().players.player.Where(p => p.status == "starter").ToList();

                    var team1ProjectedScore = CalculateProjectedScore(team1Starters, projections);
                    var team2ProjectedScore = CalculateProjectedScore(team2Starters, projections);

                    team1 += $": {team1Score} ({team1ProjectedScore:F})\n";
                    team2 += $": {team2Score} ({team2ProjectedScore:F})\n";

                    botText += $"-----\n{team1}{team2}";
                }

                await _gm.BotPost(botId, botText);
                return botText;
            }
            catch (Exception)
            {
                return "Uhh... something with MFL is broken.";
            }
        }

        private double CalculateProjectedScore(List<LiveScorePlayer> starters, IEnumerable<ProjectedPlayerScore> projections)
        {
            double totalProjectedScore = 0.0;

            foreach (var player in starters)
            {
                var projection = projections.FirstOrDefault(p => p.id == player.id)?.score ?? "0";
                player.origProjectedScore = projection;

                var pctRemaining = double.Parse(player.gameSecondsRemaining) / 3600;
                player.liveRemainingProjectedScore = double.Parse(player.score) + (double.Parse(player.origProjectedScore) * pctRemaining);

                totalProjectedScore += player.liveRemainingProjectedScore;
            }

            return totalProjectedScore;
        }

        public async Task PostFranchiseTagAmounts(string botId, int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year - 1;
                var salariesTask = _mflTranslationService.GetAllSalaries(leagueId);
                var positionsTask = _mflTranslationService.GetAllRelevantPlayers(leagueId);

                await Task.WhenAll(salariesTask, positionsTask);

                var playerSalaries = salariesTask.Result
                    .Select(s => new
                    {
                        Salary = decimal.TryParse(s.Salary, out var sal) ? sal : 0,
                        s.Id
                    })
                    .ToList();

                var tagAmounts = positionsTask.Result
                    .GroupJoin(playerSalaries, pos => pos.id, sal => sal.Id,
                        (pos, salRows) => new { pos, salRows = salRows.DefaultIfEmpty() })
                    .SelectMany(p => p.salRows.Select(sal => new
                    {
                        p.pos.id,
                        p.pos.name,
                        p.pos.position,
                        Salary = sal?.Salary ?? 0
                    }))
                    .GroupBy(p => p.position)
                    .Select(g => new
                    {
                        Position = g.Key,
                        Salary = g.OrderByDescending(p => p.Salary).Take(6).Average(p => p.Salary)
                    })
                    .ToList();

                var strForBot = $"Avg salary of top 6 at each position in {year}:\n";
                tagAmounts.ForEach(t => strForBot += $"{t.Position}: ${Decimal.Round(t.Salary)}\n");

                await _gm.BotPost(botId, strForBot);
            }
            catch (Exception)
            {
                await _gm.BotPost(botId, "Error calculating franchise tag amounts.");
            }
        }

        public async Task PostTopUpcomingFreeAgents(string botId, int leagueId, string positionRequest, int nextYearAsDefault)
        {
            var year = DateTime.UtcNow.Year;
            var pos = positionRequest.ToUpper().Trim();
            if (pos != "QB" && pos != "RB" && pos != "WR" && pos != "TE") return;

            var strForBot = $"Top {pos} Free Agents for {nextYearAsDefault}\n";
            var lookupYear = nextYearAsDefault < year + 1 ? (nextYearAsDefault - 1 ) : year; // if looking up future, use this year for lookup, if past, use past
            var avgPtsTask = _mflTranslationService.GetAveragePlayerScores(leagueId, lookupYear);
            var salariesTask = _mflTranslationService.GetAllSalaries(lookupYear);
            var playerTask = _mflTranslationService.GetAllRelevantPlayers(leagueId);
            await Task.WhenAll(avgPtsTask, playerTask, salariesTask);

            var playerInfos = playerTask.Result;
            var scores = avgPtsTask.Result;
            var contractYear = "";
            if (nextYearAsDefault == year + 1)
            {
                contractYear = "1";
            }
            else if (nextYearAsDefault == year + 2)
            {
                contractYear = "2";
            }
            else if (nextYearAsDefault == year + 3)
            {
                contractYear = "3";
            }
            else
            {
                contractYear = "1";
            }
            var relevantPlayers = salariesTask.Result.Where(_ => _.ContractYear == contractYear && _.Salary != "");

            var topScores = relevantPlayers.Select(_ => new
            {
                Id = _.Id,
                Salary = _.Salary,
                Name = playerInfos.FirstOrDefault(p => p.id == _.Id)?.name,
                Position = playerInfos.FirstOrDefault(p => p.id == _.Id)?.position,
                Score = Math.Round(Decimal.TryParse(scores.FirstOrDefault(p => p.id == _.Id)?.score, out var x) ? x : 0, 2)
            }).OrderByDescending(_ => _.Score).ToList();
            var isFirst = true;
            topScores = topScores.Where(_ => _.Position == pos).Take(12).ToList();
            if (!topScores.All(s => s.Score == 0)) topScores = topScores.Take(8).ToList();
                
            topScores.ForEach(p =>
            {
                if (isFirst) strForBot += $"{p.Name} - {p.Score} PPG\n";
                else strForBot += $"{p.Name} - {p.Score}\n";
                isFirst = false;
            });

            await _gm.BotPost(botId, strForBot);
        }

        public async Task PostDraftProjections(string botId, int leagueId, int year)
        {
            //TODO: need to check if we are predraft in the offseason, just get this year's order from that 
            var standingsTask = _mflTranslationService.GetFranchiseStandings(leagueId);
            var draftPicksTask = _mflTranslationService.GetFranchiseAssets(leagueId);
            await Task.WhenAll(standingsTask, draftPicksTask);

            if (standingsTask.Result.All(tm => tm.h2hw == "0" && tm.h2hl == "0")) //preseason
            {
                var draftPicks =
                    _mflTranslationService.GetCurrentFranchiseDraftPicks(draftPicksTask.Result)
                        .Where(pk => pk.Round == 1 || pk.Round == 2)
                        .OrderBy(pk => pk.Round)
                        .ThenBy(pk => pk.Pick).ToList();
                var botStr = "";
                draftPicks.ForEach(pick =>
                {
                    var pickNum = $"{pick.Round}.{pick.Pick.ToString("D2")}";
                    botStr += $"{pickNum} {_owners[pick.CurrentOwner]}\n";
                });
                await _gm.BotPost(botId, botStr);
            }

            else
            {
                year++;
                var draftPicks =
                    _mflTranslationService.GetFutureFranchiseDraftPicks(draftPicksTask.Result);
                var standings = standingsTask.Result
                    .Select(_ => new
                    {
                        Id = Int32.Parse(_.id),
                        Name = _owners[Int32.Parse(_.id)]
                    }).ToList();

                //go through standings twice. write a message on each one 

                for (var rd = 1; rd < 3; rd++)
                {
                    var botStr = $"Round {rd} Projection\n";
                    var pickNum = 1;
                    standings.ForEach(tm =>
                    {
                        var origSlot = tm.Id;
                        var currentPickOwner = draftPicks
                            .FirstOrDefault(d => d.Year == year && d.Round == rd && d.OriginalOwner == origSlot)?.CurrentOwner;
                        if (currentPickOwner != null)
                        {
                            botStr += $"{pickNum}) {_owners[currentPickOwner ?? 0]}";
                            botStr += origSlot == currentPickOwner ? "\n" : $" (via {_owners[origSlot]})\n";
                        }



                        pickNum++;
                    });
                    await _gm.BotPost(botId, botStr);
                }
            }
        }

        public async Task PostFutureDeadCap(string botId)
        {
            var _thisYear = DateTime.Now.Year;
            var deadCapInfo = await _leagueService.GetDeadCapData();
            var botStr = "";
            deadCapInfo.ForEach(franchise =>
            {
                var relevantYears = franchise.Amount.AsQueryable().Where(_ => _.Value != 0 && int.Parse(_.Key) >= _thisYear);
                botStr += $"{_owners[franchise.FranchiseId]}\n";
                foreach (var year in relevantYears)
                {
                    botStr += $"('{year.Key.Substring(2)}  ${year.Value}) ";
                }

                botStr += "\n";
            });
            await _gm.BotPost(botId, botStr);
        }

        public async Task PostDraftBudgets(string botId, int leagueId)
        {
             await _leagueService.MapPickBudgetToOwners(leagueId);
        }

        public async Task PostHelpMessage(string botId)
        {
            var str = $"Live scores: \"#scores\"\n" +
                      $"Standings: \"#standings\"\n" +
                      $"Player contract: \"#contract playername\"\n" +
                      $"Illegal lineups: \"#lineups\"\n" +
                      $"Upcoming free agents: \"#freeagents qb/wr/rb/te\"\n" +
                      $"Projected draft picks: \"#draft\"\n" +
                      $"Team cap space: \"#cap\"\n" +
                      $"Future dead cap: \"#dead\"\n" +
                      $"Projected draft pick budget: \"#budget\"\n" +
                      $"Franchise Tag projections: \"#tag\"\n" +
                      $"Rules: http://tinyurl.com/m8y37433";
            await _gm.BotPost(botId, str);
        }

        public async Task StrayTag(string botId)
        {
            var insult = "";

            try
            {
                insult = (await _insult.GetInsult()).insult;
            }
            catch (Exception ) {/*ignore*/}
            var insultString = string.IsNullOrEmpty(insult) ? "" : $"Otherwise...\n\n{insult}";
            await _gm.BotPost(botId, $"If you need something from me, type \"#help\". {insultString}");
        }

        public async Task BotPost(string botId, string post, bool isError = false)
        {
            await _gm.BotPost(botId, post, isError: isError);
        }
    }
}
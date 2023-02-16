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
        public Task<List<AnnualScoringData>> PostStandingsToGroup(int year);
        public Task<List<PendingTradeDTO>> PostTradeOffersToGroup(int year);
        public Task PostTradeRumor();
        public Task PostCompletedTradeToGroup();
        public Task<string> FindAndPostContract(int year, string nameSearch);
        Task<string> FindAndPostLiveScores();
        Task CheckLineupsForHoles();
        Task PostHelpMessage();
        Task PostCapSpace(int leagueId);
        Task PostDraftProjections(int year);
        Task StrayTag();
        Task PostTopUpcomingFreeAgents(string positionRequested, int year = Utils.ThisYear);
        Task PostFranchiseTagAmounts(int year = Utils.ThisYear);
        Task PostFutureDeadCap();
        Task BotPost(string post, bool isError = false);
        Task PostDraftBudgets();
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
        private static Dictionary<int, string> _owners;
        private static Dictionary<int, string> _memberIds;
        private static int _thisYear;
        
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
            _thisYear = Utils.ThisYear;
        }

        public async Task<List<AnnualScoringData>> PostStandingsToGroup(int year)
        {

            var standingsData = await _leagueService.GetStandingsV2(year);

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
                Owner = _owners[t.FranchiseId],
                Score = t.TeamStandings.Sum(s => s.PointsFor) + t.TeamStandings.Select(s => s.H2hWins * (s.Year == 2020 ? 5 : 10)).Sum()
            }).OrderByDescending(t => t.Score)
                .Take(5)
                .ToList();

            tytScores.ForEach(s =>
            {
                tytString = $"{tytString}{s.Owner} - {s.Score}\n";
            });
            await _gm.BotPost(strForBot);
            await _gm.BotPost(tytString);
            return standings;
        }

        // I dont know what i was doing here?
/*        public int GetAdjustedWins(int h2hWins, int vp)
        {
            return h2hWins + vp - (h2hWins * 2);
        }*/

        public async Task<List<PendingTradeDTO>> PostTradeOffersToGroup(int year)
        {
            var tenMinDuration = new TimeSpan(0, 0, 10, 0);
            var trades = await _leagueService.FindPendingTrades(year);
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
                        var tagName = memberList.Find(m => m.user_id == _memberIds[t.offeredTo]);
                        var tagString = $"@{tagName.nickname}";
                        strForBot = ", you have a pending trade offer!";
                        await _gm.BotPostWithTag(strForBot, tagString, tagName.user_id);
                    }
                });
            }
            return trades;
        }

        public async Task PostCompletedTradeToGroup()
        {
            DateTime tenMinAgo = DateTime.Now.AddMinutes(-11);

            var tradeInfoList = await _mflTranslationService.GetCompletedTrades();
            //TODO: CHECK FOR NULL?? it would mean it could not serialize it as either type
            
            foreach (var trade in tradeInfoList)
            {
                if (trade != null && !string.IsNullOrEmpty(trade.franchise2))
                {
                    var tradeTime = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(trade.timestamp));
                    // check if trade was not in the last 10 minutes to bail early
                    if (tradeTime <= tenMinAgo) continue;
                    var strForBot = await _rumor.GetCompletedTradeString(trade);
                    await _gm.BotPost(strForBot);
                }
            }
        }

        public async Task PostTradeRumor()
        {
            // TODO: make the rumor service a string builder service.

            var baitList = await _mflTranslationService.GetNewTradeBait();

            foreach (var post in baitList)
            {
                if (post?.timestamp != null)
                {
                    var strForBot = "";
                    var postDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(post.timestamp));
                    if (postDate > DateTime.Now.AddMinutes(-11))
                    {
                        strForBot = await _rumor.GetTradeBaitString(post);
                        await _gm.BotPost(strForBot);
                    }
                }
            }
        }

        public async Task<string> FindAndPostContract(int year, string nameSearch)
        {
            if (nameSearch.EndsWith(" ")) nameSearch = nameSearch.Trim();
            var hits = await _mflTranslationService.GetRosteredPlayersByName(year, nameSearch);
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
            await _gm.BotPost(stringForBot);
            return stringForBot;
        }

        public async Task CheckLineupsForHoles()
        {
            var thisWeek = await _mflTranslationService.GetThisLeagueWeek();

            // task when all get injuries and byes get scores with that week num
            var scoresTask = _mflTranslationService.GetLiveScoresForFranchises(thisWeek);
            var byesTask = _mflTranslationService.GetByesThisWeek(thisWeek);
            var injuriesTask = _mflTranslationService.GetInjurredPlayerIdsThisWeek(thisWeek);
            var allPlayersTask = _mflTranslationService.GetAllRelevantPlayers();
            var groupTask = _gm.GetMemberIds();

            try
            {
                await Task.WhenAll(scoresTask, injuriesTask, byesTask, allPlayersTask, groupTask);
            }
            catch (Exception)
            {
                await _gm.BotPost("I don't know MFL is busted.");
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
                    var tagName = memberList.Find(m => m.user_id == _memberIds[Int32.Parse(franch.id)]);
                    var tagString = $"@{tagName?.nickname}";
                    await _gm.BotPostWithTag(botStr, tagString, tagName?.user_id ?? "");
                    brokenTeams.Add(franch.id);
                }
            }
            //TODO: mark if tankin'?
            if (!brokenTeams.Any()) await _gm.BotPost("Lineups are all straight, mate.");
        }

        public async Task PostCapSpace(int leagueId)
        {
            var botStr = "Current Cap Space (Next Year)\n";
           
            //get total salaries this season + adjustments, subtract from team budget
            var salaryAdjustmentsTask = _mflTranslationService.GetSalaryAdjustments(leagueId, _thisYear);
            var salariesTask = _mflTranslationService.GetFranchiseSalaries();
            var leagueTask = _mflTranslationService.GetTeamAdjustedSalaryCaps();
            try
            {
                await Task.WhenAll(salariesTask, leagueTask, salaryAdjustmentsTask);
            }
            catch (Exception e)
            {
                await _gm.BotPost("I'm having trouble reaching MFL. Try again later.");
            }


            var thisSznAdj = salaryAdjustmentsTask.Result;
            var adjustments = await _leagueService.GetDeadCapData();
            // add up salaries for this year - but dont forget to * .5  and .4 for taxi and IR
            
            var orderedSummaries = _dataHelper.CreateFranchiseCapSummaries(salariesTask.Result, adjustments);
            
            orderedSummaries.ForEach(tm =>
            {
                var teamAdj = thisSznAdj.Where(adj => Int32.Parse(adj.Franchise_Id) == tm.Id)
                    .Select(adj => Decimal.TryParse(adj.Amount, out var x) ? x : 0).Sum();
                botStr += $"{_owners[tm.Id]}: " +
                          $"${leagueTask.Result.First(_ => _.Id == tm.Id).SalaryCapAmount - (tm.CurrentRosterSalary + tm.CurrentTaxiSalary + tm.CurrentIRSalary) - teamAdj} " +
                          $"(${500 - (tm.NextYearRosterSalary + (tm.DeadCapData.ContainsKey((_thisYear + 1).ToString()) ? tm.DeadCapData[(_thisYear + 1).ToString()] : 0))})\n";
            });
            await _gm.BotPost(botStr);
        }

        public async Task<string> FindAndPostLiveScores()
        {
            var thisWeek = await _mflTranslationService.GetThisLeagueWeek();
            var botText = "Live Scores (Live Projections)\n";
            var matchupScoresTask = _mflTranslationService.GetLiveScoresForMatchups(thisWeek);
            var scoreProjectionsTask = _mflTranslationService.GetProjections(thisWeek);
            try
            {
                await Task.WhenAll(matchupScoresTask, scoreProjectionsTask);
            }
            catch (Exception)
            {
                return "Uhh... something with MFL is broken.";
            }            

            var matchups = matchupScoresTask.Result;
            var projections = scoreProjectionsTask.Result;
            matchups.ForEach(_ =>
            {
                _owners.TryGetValue(Int32.Parse(_.franchise.First().id), out var tm1);
                _owners.TryGetValue(Int32.Parse(_.franchise.Last().id), out var tm2);
                var success = Double.TryParse(_.franchise.First().score, out var tm1Score);
                var success2 = Double.TryParse(_.franchise.Last().score, out var tm2Score);
                
                // go through all the "starter"s on each team and add up their projected score.
                var tm1Starters = _.franchise.First().players.player.Where(p => p.status == "starter").ToList();
                var tm2Starters = _.franchise.Last().players.player.Where(p => p.status == "starter").ToList();

                tm1Starters.ForEach(s => s.origProjectedScore = projections.FirstOrDefault(p => p.id == s.id)?.score ?? "0");
                tm2Starters.ForEach(s => s.origProjectedScore = projections.FirstOrDefault(p => p.id == s.id)?.score ?? "0");
                
                // for each player
                // get % of game left for each player (out of 3600) multiply 
                // add actual score + remaining projection
                var tm1ProjectedScore = 0.0;
                var tm2ProjectedScore = 0.0;
                tm1Starters.ForEach(p =>
                {
                    var pctRemaining = Double.Parse(p.gameSecondsRemaining) / 3600;
                    p.liveRemainingProjectedScore = Double.Parse(p.score) + (Double.Parse(p.origProjectedScore) * pctRemaining);
                    tm1ProjectedScore += p.liveRemainingProjectedScore;
                });
                tm2Starters.ForEach(p =>
                {
                    var pctRemaining = Double.Parse(p.gameSecondsRemaining) / 3600;
                    p.liveRemainingProjectedScore = Double.Parse(p.score) + (Double.Parse(p.origProjectedScore) * pctRemaining);
                    tm2ProjectedScore += p.liveRemainingProjectedScore;
                });
                
                if (!success || !success2) return;
                tm1 += $": {tm1Score} ({tm1ProjectedScore.ToString("F")})\n";
                tm2 += $": {tm2Score} ({tm2ProjectedScore.ToString("F")})\n";
                botText += $"-----\n{tm1}{tm2}";
                
                
            });
            await _gm.BotPost(botText);
            return botText;
        }

        public async Task PostFranchiseTagAmounts(int year = Utils.ThisYear - 1)
        {
            var salariesTask = _mflTranslationService.GetAllSalaries();
            var positionTask = _mflTranslationService.GetAllRelevantPlayers();
            await Task.WhenAll(salariesTask, positionTask);

            var positionIds = positionTask.Result;
            var playerSalaries = salariesTask.Result.Select(_ => new
            {
                Salary = Decimal.TryParse(_.Salary, out var sal) ? sal : 0,
                Id = _.Id
            }).ToList();
            
            var tagAmounts = positionIds.GroupJoin(playerSalaries, pos => pos.id, salary => salary.Id, (pos, salaryRows) => new
                {
                    pos,
                    salaryRows = salaryRows.DefaultIfEmpty()
                }).SelectMany(p => p.salaryRows.Select(sal => new
                {
                    Id = p.pos.id,
                    Name = p.pos.name,
                    Position = p.pos.position,
                    Salary = sal?.Salary ?? 0
                }).ToList())
                .OrderByDescending(p => p.Salary)
                .GroupBy(p => p.Position)
                .Select(pos => new
                {
                    Position = pos.Key,
                    Salary = pos.Take(6).Average(_ => _.Salary)
                })
                .ToList();

            var strForBot = $"Avg salary of top 6 at each position in {year}:\n";
            tagAmounts.ForEach(t =>
            {
                strForBot += $"{t.Position}: ${Decimal.Round(t.Salary)}\n";
            });

            await _gm.BotPost(strForBot);
        }

        public async Task PostTopUpcomingFreeAgents(string positionRequest, int year = Utils.ThisYear)
        {
            var pos = positionRequest.ToUpper().Trim();
            if (pos != "QB" && pos != "RB" && pos != "WR" && pos != "TE") return;

            var strForBot = $"Top {pos} Free Agents for {year + 1}\n";
            var avgPtsTask = _mflTranslationService.GetAveragePlayerScores(year);
            var salariesTask = _mflTranslationService.GetAllSalaries();
            var playerTask = _mflTranslationService.GetAllRelevantPlayers();
            await Task.WhenAll(avgPtsTask, playerTask, salariesTask);

            var playerInfos = playerTask.Result;
            var scores = avgPtsTask.Result;
            var relevantPlayers = salariesTask.Result.Where(_ => _.ContractYear == "1" && _.Salary != "");

            var topScores = relevantPlayers.Select(_ => new
            {
                Id = _.Id,
                Salary = _.Salary,
                Name = playerInfos.FirstOrDefault(p => p.id == _.Id)?.name,
                Position = playerInfos.FirstOrDefault(p => p.id == _.Id)?.position,
                Score = Math.Round(Decimal.TryParse(scores.FirstOrDefault(p => p.id == _.Id)?.score, out var x) ? x : 0, 2)
            }).OrderByDescending(_ => _.Score).ToList();
            var isFirst = true;
            topScores.Where(_ => _.Position == pos).Take(8).ToList().ForEach(p =>
            {
                if (isFirst) strForBot += $"{p.Name} - {p.Score} PPG\n";
                else strForBot += $"{p.Name} - {p.Score}\n";
                isFirst = false;
            });
            await _gm.BotPost(strForBot);
        }

        public async Task PostDraftProjections(int year)
        {
            //TODO: need to check if we are predraft in the offseason, just get this year's order from that 
            var standingsTask = _mflTranslationService.GetFranchiseStandings();
            var draftPicksTask = _mflTranslationService.GetFranchiseAssets();
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
                await _gm.BotPost(botStr);
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
                    await _gm.BotPost(botStr);
                }
            }
        }

        public async Task PostFutureDeadCap()
        {
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
            await _gm.BotPost(botStr);
        }

        public async Task PostDraftBudgets()
        {
             await _leagueService.MapPickBudgetToOwners();
        }

        public async Task PostHelpMessage()
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
            await _gm.BotPost(str);
        }

        public async Task StrayTag()
        {
            var insult = "";

            try
            {
                insult = (await _insult.GetInsult()).insult;
            }
            catch (Exception ) {/*ignore*/}
            var insultString = string.IsNullOrEmpty(insult) ? "" : $"Otherwise...\n\n{insult}";
            await _gm.BotPost($"If you need something from me, type \"#help\". {insultString}");
        }

        public async Task BotPost(string post, bool isError = false)
        {
            await _gm.BotPost(post, isError: isError);
        }
    }
}
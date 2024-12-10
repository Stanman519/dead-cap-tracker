using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
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
        public Task<List<PendingTradeDTO>> PostTradeOffersToGroup(string botId, int leagueId, int year, string groupId);
        public Task PostTradeRumor(string botId, int leagueId);
        public Task PostCompletedTradeToGroup(string botId, int leagueId);
        public Task<string> FindAndPostContract(string botId, int leagueId, int year, string nameSearch);
        Task<string> FindAndPostLiveScores(string botId, int leagueId);
        Task CheckLineupsForHoles(string botId, int leagueId, string groupId);
        Task PostHelpMessage(string botId);
        Task PostCapSpace(string botId, int leagueId);
        Task PostDraftProjections(string botId, int leagueId, int year);
        Task StrayTag(string botId);
        Task PostTopUpcomingFreeAgents(string botId, int leagueId, string positionRequested, int year);
        Task PostFranchiseTagAmounts(string botId, int leagueId);
        Task PostFutureDeadCap(string botId, int leagueId);
        Task BotPost(string botId, string post, bool isError = false);
        Task PostDraftBudgets(string botId, int leagueId);
        Task OptimizeLineup(string botId, int leagueId, string gmId);
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
        private readonly DeadCapTrackerContext _db;

        public GroupMeRequestService(IMflTranslationService mflTranslationService, 
            IDataSetHelperService dataHelper, 
            IGroupMePostRepo gm, 
            ILeagueService leagueService, 
            IRumorService rumor, 
            DeadCapTrackerContext db,
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
            _db = db;
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
                strForBot = $"{strForBot}{_owners[leagueId][s.FranchiseId]}  ({s.VictoryPoints} VP)  {s.H2hWins}-{s.H2hLosses}    {s.PointsFor} pts\n";
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

        public async Task<List<PendingTradeDTO>> PostTradeOffersToGroup(string botId, int leagueId, int year, string groupId)
        {
            var tenMinDuration = new TimeSpan(0, 0, 10, 0);
            var trades = await _leagueService.FindPendingTrades(leagueId, year);
            var memberList = (await _gm.GetMemberIds(groupId)).response.members;

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

            foreach (var trade in tradeInfoList)
            {
                if (trade != null && !string.IsNullOrEmpty(trade.franchise2))
                {
                    var trader1 = int.TryParse(trade.franchise, out var x) ? x : 0;
                    var trader2 = int.TryParse(trade.franchise2, out var y) ? y : 0;
                    var capEats = _db.CapEatCandidates
                        .Where(c => c.Proposal.Expires.ToString() == trade.expires && ((trader1 == c.Proposal.SenderId && trader2 == c.Proposal.ReceiverId) || 
                        (trader2 == c.Proposal.SenderId && trader1 == c.Proposal.ReceiverId))).ToList();

                    if (capEats.Count > 0)
                    {
                        var playerInfos = await _mflTranslationService.GetMultiMflPlayers(leagueId, string.Join(",", capEats.Select(f => f.MflPlayerId)));
                        // implement this year cap eats via salary adjustments
                        var thisYearEats = capEats.Where(c => c.Year == tenMinAgo.Year && c.CapAdjustment != 0).ToList();
                        
                        if (thisYearEats.Count > 0)
                        {
                            // need to actually make 2 adjustments for each capEat.  one to rebate the receiver and one to fine the sender.
                            var penalties = thisYearEats.Select(_ => {

                                var thisPlayer = playerInfos.FirstOrDefault(p => p.id == _.MflPlayerId.ToString());
                                return new SalaryAdjustment
                                {
                                    //explanation=\"{s.reason} {s.player.LastName}, {s.player.FirstName} {s.player.Team} {s.player.Position} (Salary: ${s.player.Salary}, years left: {s.length})\"/>");
                                    adjustmentAmount = _.CapAdjustment,
                                    franchiseId = _.EaterId.ToString("D4"),
                                    length = 1,
                                    player = new PlayerDTO
                                    {
                                        FirstName = thisPlayer?.name.Split(",")[1] ?? "ERROR",
                                        LastName = thisPlayer?.name.Split(",")[0] ?? "ERROR",
                                        Team = thisPlayer?.team ?? "N/A",
                                        Position = thisPlayer?.position ?? "N/A",
                                        Salary = _.CapAdjustment,
                                        Length = 1,
                                        MflId = int.TryParse(thisPlayer.id, out var p) ? p : 0
                                    },
                                    reason = "SENDING"

                                };
                            });
                            var rebates = thisYearEats.Select(_ => {

                                var thisPlayer = playerInfos.FirstOrDefault(p => p.id == _.MflPlayerId.ToString());
                                return new SalaryAdjustment
                                {
                                    adjustmentAmount = -_.CapAdjustment,
                                    franchiseId = _.ReceiverId.ToString("D4"),
                                    length = 1,
                                    player = new PlayerDTO
                                    {
                                        FirstName = thisPlayer?.name.Split(",")[1] ?? "ERROR",
                                        LastName = thisPlayer?.name.Split(",")[0] ?? "ERROR",
                                        Team = thisPlayer?.team ?? "N/A",
                                        Position = thisPlayer?.position ?? "N/A",
                                        Salary = _.CapAdjustment,
                                        Length = 1,


                                    },
                                    reason = "RECEIVING"
                                };
                            });
                            var salaryAdj = penalties.ToList();

                            salaryAdj.AddRange(rebates);
                            //check here to make sure these havent already happened
                            await _mflTranslationService.BuildAndPostSalaryAdjustments(leagueId, salaryAdj, tenMinAgo.Year);
                        }

                        // future years go into db
                        var futureYearEats = capEats.Where(c => c.Year != tenMinAgo.Year).ToList();

                        if (futureYearEats.Count > 0 && _db.Transactions.FirstOrDefault(t => futureYearEats.Select(f => f.Id).ToList().Contains(t.CapEatId ?? -1)) != null)
                        {

                            var newAdds = futureYearEats.Select(f =>
                            {
                                var player = playerInfos.FirstOrDefault(p => p.id == f.MflPlayerId.ToString());
                                return new Repositories.Transaction
                                {
                                    Amount = f.CapAdjustment,
                                    Franchiseid = f.EaterId,
                                    Leagueid = f.LeagueId,
                                    Playername = player?.name ?? "",
                                    Position = player?.position ?? "",
                                    Salary = f.CapAdjustment,
                                    Yearoftransaction = tenMinAgo.Year,
                                    Team = player?.team ?? "",
                                    Timestamp = tenMinAgo,
                                    Transactionid = f.Id,
                                    Years = 1,
                                    CapEatId = f.Id

                                };
                            });
                            try
                            {
                                _db.Transactions.AddRange(newAdds);
                                _db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                await _gm.BotPost("", $"failed to load cap eats to db. {ex.Message}", true);
                            }
                        }
                    }
                    var tradeTime = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(trade.timestamp));
                    // check if trade was not in the last 10 minutes to bail early
                    if (tradeTime <= tenMinAgo) continue;
                    var strForBot = await _rumor.GetCompletedTradeString(leagueId, trade, capEats);  
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

        public async Task CheckLineupsForHoles(string botId, int leagueId, string groupId)
        {
            var thisWeek = await _mflTranslationService.GetThisLeagueWeek(leagueId);

            // task when all get injuries and byes get scores with that week num
            var scoresTask = _mflTranslationService.GetLiveScoresForFranchises(leagueId, thisWeek);
            var byesTask = _mflTranslationService.GetByesThisWeek(thisWeek);
            var injuriesTask = _mflTranslationService.GetInjurredPlayerIdsThisWeek(thisWeek);
            var allPlayersTask = _mflTranslationService.GetAllRelevantPlayers(leagueId);
            var groupTask = _gm.GetMemberIds(groupId);

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
                if (!franch.players.player.Any()) // no starters
                {
                    var tagName = memberList.Find(m => m.user_id == _memberIds[leagueId][Int32.Parse(franch.id)]);
                    var tagString = $"@{tagName?.nickname}";
                    await _gm.BotPostWithTag(botId, botStr, tagString, tagName?.user_id ?? "");
                    brokenTeams.Add(franch.id);
                }
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
            if (!brokenTeams.Any()) await _gm.BotPost(botId, "All lineups seem valid... but can you really trust me?");
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
                var medianActual = new List<double>();
                var medianProj = new List<double>();
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
                    medianActual.Add(team1Score);
                    medianActual.Add(team2Score);
                    medianProj.Add(team1ProjectedScore);
                    medianProj.Add(team2ProjectedScore);
                    botText += $"-----\n{team1}{team2}";
                }
                botText += $"\n-----\nMedian: {medianActual.GetMedian():F} ({medianProj.GetMedian():F})";
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



        public async Task OptimizeLineup(string botId, int leagueId, string gmId)
        {
            var mflFranchiseIdsAndGmIds = Utils.memberIds[leagueId];

            var mflFranchiseId = mflFranchiseIdsAndGmIds.FirstOrDefault(f => f.Value.Contains(gmId)).Key.ToString("D4");
            var thisWeek = await  _mflTranslationService.GetThisLeagueWeek(leagueId);

            try
            {
                var rostersTask = _mflTranslationService.GetLiveScoresForFranchises(leagueId, "");
                var projectionsTask = _mflTranslationService.GetProjections(leagueId, thisWeek);

                await Task.WhenAll(rostersTask,  projectionsTask);
                // get MFL rosters (does this come with player position?)

                var scoreFranchise = rostersTask.Result.First(r => r.id == mflFranchiseId);
                var yetToPlay = scoreFranchise.playersYetToPlay;
                var usablePlayers = scoreFranchise.players.player;

                var queryStr = string.Join(",", usablePlayers.Select(_ => _.id));

                var positions = await _mflTranslationService.GetMultiMflPlayers(leagueId, queryStr);
                int qbRequired = 1, rbRequired = 2, wrRequired = 3, teRequired = 1, flexRequired = 3;
                // get mfl player projections
                var joined = usablePlayers
                    .GroupJoin(projectionsTask.Result, play => play.id, proj => proj.id, (play, proj) => new JoinedLineupPlayer
                    {
                        Player = play,
                        Projection = proj.FirstOrDefault() ?? new ProjectedPlayerScore { 
                            id= play.id, 
                            score= "0"}
                    })
                    .GroupJoin(positions, joinedPlayer => joinedPlayer.Player.id, pos => pos.id, (joinedPlayer, pos) => new JoinedLineupPlayer
                    {
                        Player = joinedPlayer.Player,
                        Projection = joinedPlayer.Projection,
                        Position = pos.FirstOrDefault() ?? new Player(),
                        IsLockedStart = (joinedPlayer.Player.status == "starter" && 
                            ((int.TryParse(joinedPlayer.Player.gameSecondsRemaining, out var x) ? x : 0) < 3600 ||
                            (decimal.TryParse(joinedPlayer.Player.score, out var y) ? y : 0) > 0)),
                        IsLockedBench = (joinedPlayer.Player.status == "nonstarter") && 
                            ((int.TryParse(joinedPlayer.Player.gameSecondsRemaining, out var z) ? z : 0) < 3600 ||
                            (decimal.TryParse(joinedPlayer.Player.score, out var r) ? r : 0) > 0)
                    }).ToList()
                    .Where(p => !p.IsLockedBench)
                    .OrderByDescending(p => p.IsLockedStart)
                    .ThenByDescending(p => decimal.TryParse(p.Projection.score, out var q) ? q : 0).ToList();
                //marry the rosters (with position) to the projections order by projection


                var starters = new List<JoinedLineupPlayer>();
                var flex = new List<JoinedLineupPlayer>();

                int qbCount = 0, rbCount = 0, wrCount = 0, teCount = 0;
                int qbMax = 2, rbMax = 5, wrMax = 6, teMax = 4;

                var strBuilder = new StringBuilder();

                strBuilder.Append("Successfully Optimized Lineup");
                foreach (var player in joined)
                {
                    var realScore = player.IsLockedStart ? (decimal.TryParse(player.Player.score, out var scr) ? scr : 0) : 0;
                    var scoreStr = player.IsLockedStart ? realScore + " scored" : player.Projection.score + " proj";

                    // Add players based on their position requirements
                    switch (player.Position)
                    {
                        case var pos when pos.position.Contains("QB") && qbCount < qbRequired:
                            starters.Add(player);
                            qbCount++;
                            strBuilder.Append($"\n{player.Position.position} {player.Position.name} - {scoreStr}");
                            break;

                        case var pos when pos.position.Contains("RB") && rbCount < rbRequired:
                            starters.Add(player);
                            rbCount++;
                            strBuilder.Append($"\n{player.Position.position} {player.Position.name} - {scoreStr}");
                            break;

                        case var pos when pos.position.Contains("WR") && wrCount < wrRequired:
                            starters.Add(player);
                            wrCount++;
                            strBuilder.Append($"\n{player.Position.position} {player.Position.name} - {scoreStr}");
                            break;

                        case var pos when pos.position.Contains("TE") && teCount < teRequired:
                            starters.Add(player);
                            teCount++;
                            strBuilder.Append($"\n{player.Position.position} {player.Position.name} - {scoreStr}");
                            break;

                        default:
                            // If positional requirements met, add to flex if there’s space
                            if (flex.Count < flexRequired)
                            {
                                var position = player.Position.position;
                                if ((position.Contains("QB") && qbCount < qbMax) ||
                                    (position.Contains("WR") && wrCount < wrMax) ||
                                     (position.Contains("TE") && teCount < teMax) ||
                                    (position.Contains("RB") && rbCount < rbMax))
                                {
                                    switch (position)
                                    {
                                        case var p when p?.Contains("QB") ?? false:
                                            qbCount++;
                                            break;
                                        case var p when p?.Contains("RB") ?? false:
                                            rbCount++;
                                            break;
                                        case var p when p?.Contains("WR") ?? false:
                                            wrCount++;
                                            break;
                                        case var p when p?.Contains("TE") ?? false:
                                            teCount++;
                                            break;
                                        default:
                                            break;
                                    }
                                    flex.Add(player);
                                    strBuilder.Append($"\n{player.Position.position} {player.Position.name} - {scoreStr}");
                                }

                            }
                            break;
                    }

                    // Break early if all required slots are filled
                    if (qbCount >= qbRequired && rbCount >= rbRequired && wrCount >= wrRequired &&
                        teCount >= teRequired && flex.Count >= flexRequired)
                    {
                        break;
                    }
                }

                // Combine starters and flex
                starters.AddRange(flex);

                if (starters.Count < 10)
                {
                    if (starters.Where(_ => _.Position.position.Contains("QB")).Count() < qbRequired)
                    {
                        throw new Exception("Not enough active QBs");
                    }
                    if (starters.Where(_ => _.Position.position.Contains("TE")).Count() < teRequired)
                    {
                        throw new Exception("Not enough active TEs");
                    }
                    if (starters.Where(_ => _.Position.position.Contains("WR")).Count() < wrRequired)
                    {
                        throw new Exception("Not enough active WRs");
                    }
                    if (starters.Where(_ => _.Position.position.Contains("RB")).Count() < rbRequired)
                    {
                        throw new Exception("Not enough active RBs");
                    }
                    else
                    {
                        throw new Exception("You do not have enough active viable players to start a full lineup.");
                    }
                }
                var starterIds = string.Join(",", starters.Select(s => s.Player.id));
                //SUBMIT LINEUP TO MFL
                await _mflTranslationService.SetLineupForFranchise(leagueId, starterIds, mflFranchiseId, botId);

                await _gm.BotPost(botId, strBuilder.ToString());
                //POST BOT STRING ON SUCCESS

                }
            catch (Exception e)
            {
                await _gm.BotPost(botId, "Lineup submission failed. " + e.Message);
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
            var salariesTask = _mflTranslationService.GetAllSalaries(leagueId);
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
                    botStr += $"{pickNum} {_owners[leagueId][pick.CurrentOwner]}\n";
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
                        Name = _owners[leagueId][Int32.Parse(_.id)]
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
                            botStr += $"{pickNum}) {_owners[leagueId][currentPickOwner ?? 0]}";
                            botStr += origSlot == currentPickOwner ? "\n" : $" (via {_owners[leagueId][origSlot]})\n";
                        }



                        pickNum++;
                    });
                    await _gm.BotPost(botId, botStr);
                }
            }
        }

        public async Task PostFutureDeadCap(string botId, int leagueId)
        {
            var _thisYear = DateTime.Now.Year;
            var deadCapInfo = await _leagueService.GetDeadCapData();
            var botStr = "";
            deadCapInfo.ForEach(franchise =>
            {
                var relevantYears = franchise.Amount.AsQueryable().Where(_ => _.Value != 0 && int.Parse(_.Key) >= _thisYear);
                botStr += $"{_owners[leagueId][franchise.FranchiseId]}\n";
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
    public static class SomeExtensions
    {
        public static double GetMedian(this List<double> source)
        {
            // Create a copy of the input, and sort the copy
            double[] temp = source.ToArray();
            Array.Sort(temp);

            int count = temp.Length;
            if (count == 0)
            {
                throw new InvalidOperationException("Empty collection");
            }
            else if (count % 2 == 0)
            {
                // count is even, average two middle elements
                double a = temp[count / 2 - 1];
                double b = temp[count / 2];
                return (a + b) / 2;
            }
            else
            {
                // count is odd, return the middle element
                return temp[count / 2];
            }
        }
        public static T PopAt<T>(this List<T> list, int index)
        {
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }
    }
    public class JoinedLineupPlayer
    {
        public LiveScorePlayer Player { get; set; }
        public ProjectedPlayerScore Projection { get; set; }
        public Player Position { get; set; }
        public bool IsLockedStart { get; set; }
        public bool IsLockedBench { get; set; }
    }
}
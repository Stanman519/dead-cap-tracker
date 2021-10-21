using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Repositories;
using RestEase;

namespace DeadCapTracker.Services
{
    public interface IGroupMeRequestService
    {
        public Task<List<TeamStandings>> PostStandingsToGroup(int year);
        public Task<List<PendingTradeDTO>> PostTradeOffersToGroup(int year);
        public Task PostTradeRumor();
        public Task PostCompletedTradeToGroup();
        Task BotPost(string text);
        public Task<string> FindAndPostContract(int year, string nameSearch);
        Task<string> FindAndPostLiveScores();
        Task CheckLineupsForHoles();
        Task PostHelpMessage();
        Task PostCapSpace();
        Task StrayTag();
    }
    
    public class GroupMeRequestRequestService : IGroupMeRequestService
    {
        private IGroupMeApi _gmApi;
        private readonly IMflApi _mfl;
        private readonly IGlobalMflApi _globalMflApi;
        private readonly ILeagueService _leagueService;
        private readonly IRumorService _rumor;
        private readonly IInsultApi _insult;
        private static Dictionary<int, string> _owners;
        private static Dictionary<int, string> _memberIds;
        private static int _thisYear;
        
        public GroupMeRequestRequestService(IGroupMeApi gmApi, IMflApi mfl, IGlobalMflApi globalMflApi, ILeagueService leagueService, IRumorService rumor, IInsultApi insult)
        {
            _gmApi = gmApi;
            _mfl = mfl;
            _globalMflApi = globalMflApi;
            _leagueService = leagueService;
            _rumor = rumor;
            _insult = insult;
            _owners = Utils.owners;
            _memberIds = Utils.memberIds;
            _thisYear = Utils.ThisYear;
        }

        public async Task<List<TeamStandings>> PostStandingsToGroup(int year)
        {
            var standings = (await _leagueService.GetStandings(year))
                .OrderByDescending(_ => _.VictoryPoints2)
                .ThenByDescending(_ => _.H2hWins2)
                .ThenByDescending(_ => _.PointsFor2)
                .ToList();
            var strForBot = "STANDINGS \n";
            var tytString = "Tri-Year Trophy Presented by Taco Bell\nTOP 5\n";
            standings.ForEach(s =>
            {
                strForBot = $"{strForBot}{_owners[s.FranchiseId]}  ({s.VictoryPoints2} VP)  {s.H2hWins2}-{s.H2hLosses2}    {s.PointsFor2} pts\n";
            });
            var tytScores = standings.Select(t => new TYTScore
                {
                    Owner = _owners[t.FranchiseId],
                    Score = (t.H2hWins1 * 5 + t.PointsFor1) + ((GetAdjustedWins(t.H2hWins2, t.VictoryPoints2) * 5) + t.PointsFor2)
                                                            + ((GetAdjustedWins(t.H2hWins3, t.VictoryPoints3) * 5) + t.PointsFor3)
                }).OrderByDescending(t => t.Score)
                .Take(5)
                .ToList();

            tytScores.ForEach(s =>
            {
                tytString = $"{tytString}{s.Owner} - {s.Score}\n";
            });
            await BotPost(strForBot);
            await BotPost(tytString);
            return standings;
        }

        public int GetAdjustedWins(int h2hWins, int vp)
        {
            return h2hWins + vp - (h2hWins * 2);
        }

        public async Task<List<PendingTradeDTO>> PostTradeOffersToGroup(int year)
        {
            var tenMinDuration = new TimeSpan(0, 0, 10, 0);
            var trades = await _leagueService.FindPendingTrades(year);
            var group = await _gmApi.GetMemberIds();
            var memberList = group.response.members;
            
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
                        await BotPostWithTag(strForBot, tagString, tagName.user_id);
                    }
                });
            }
            return trades;
        }

        public async Task PostCompletedTradeToGroup()
        {
            var deserializer = new JsonResponseDeserializer();
            var info = new ResponseDeserializerInfo();
            var tradeRes = await _mfl.GetRecentTrade();
            var strForBot = "";
            var jsonString = await tradeRes.Content.ReadAsStringAsync();
            var owner1 = "";
            var owner2 = "";
            var assets1 = "";
            var assets2 = "";

            try
            {
                //Single
                var tradeSingle = deserializer.Deserialize<TradeTransactionSingle>(jsonString, tradeRes, info)
                    .transactions.transaction;
                DateTime tenMinAgo = DateTime.Now.AddMinutes(-11);
                var tradeTime = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(tradeSingle.timestamp));
                // check if trade was not in the last 10 minutes to bail early
                if (tradeTime < tenMinAgo)
                {
                    return;
                }
                _owners.TryGetValue(Int32.Parse(tradeSingle.franchise), out owner1);
                _owners.TryGetValue(Int32.Parse(tradeSingle.franchise2), out owner2);
                strForBot += $"{_rumor.GetSources()}{owner1} and {owner2} have completed a trade. \n";
                
                var multiplePlayers1 = _rumor.CheckForMultiplePlayers(tradeSingle.franchise1_gave_up);
                var multiplePlayers2 = _rumor.CheckForMultiplePlayers(tradeSingle.franchise2_gave_up);
                assets1 = multiplePlayers1 ? await _rumor.ListTradeInfoWithMultiplePlayers(tradeSingle.franchise1_gave_up) : await _rumor.ListTradeInfoWithSinglePlayer(tradeSingle.franchise1_gave_up);
                assets2 = multiplePlayers2 ? await _rumor.ListTradeInfoWithMultiplePlayers(tradeSingle.franchise2_gave_up) : await _rumor.ListTradeInfoWithSinglePlayer(tradeSingle.franchise2_gave_up);
                
                strForBot += $"{owner1} sends: \n{assets1} \n{owner2} sends: \n{assets2}";
                
                await BotPost(strForBot);
                return;
            }
            catch (Exception) {Console.WriteLine("not a single trade");}

            try
            {
                //Multiple
                var multiTrade = deserializer.Deserialize<TradeTransactionMulti>(jsonString, tradeRes, info)
                    .transactions.transaction;
                var tenMinAgo = DateTime.Now.AddMinutes(-11);
                foreach (var trade in multiTrade)
                {
                    var tradeTime = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(trade.timestamp));
                    // check if trade was not in the last 10 minutes to bail early
                    if (tradeTime >= tenMinAgo)
                    {
                        _owners.TryGetValue(Int32.Parse(trade.franchise), out owner1);
                        _owners.TryGetValue(Int32.Parse(trade.franchise2), out owner2);
                        strForBot += $"{_rumor.GetSources()}{owner1} and {owner2} have completed a trade. \n";
                
                        var multiplePlayers1 = _rumor.CheckForMultiplePlayers(trade.franchise1_gave_up);
                        var multiplePlayers2 = _rumor.CheckForMultiplePlayers(trade.franchise2_gave_up);
                        assets1 = multiplePlayers1 ? await _rumor.ListTradeInfoWithMultiplePlayers(trade.franchise1_gave_up) : await _rumor.ListTradeInfoWithSinglePlayer(trade.franchise1_gave_up);
                        assets2 = multiplePlayers2 ? await _rumor.ListTradeInfoWithMultiplePlayers(trade.franchise2_gave_up) : await _rumor.ListTradeInfoWithSinglePlayer(trade.franchise2_gave_up);
                
                        strForBot += $"{owner1} sends: \n{assets1} \n{owner2} sends: \n{assets2}";
                    
                        await BotPost(strForBot);
                    }
                }
            }
            catch (Exception) {Console.WriteLine("not a multi trade");}
        }

        public async Task PostTradeRumor()
        {
            var deserializer = new JsonResponseDeserializer();
            var info = new ResponseDeserializerInfo();
            var res = await _mfl.GetTradeBait();
            var strForBot = "";
            var jsonString = await res.Content.ReadAsStringAsync();
            
            try
            {
                var tradeBait = deserializer.Deserialize<TradeBaitParent>(jsonString, res, info).tradeBaits.tradeBait;
                strForBot += _rumor.GetSources();
                _owners.TryGetValue(Int32.Parse(tradeBait.franchise_id), out var ownerName);
                strForBot += $"{ownerName} ";
                // check if this is a new post or not.
                var postDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(tradeBait.timestamp));
                if (postDate < DateTime.Now.AddMinutes(-11)) return;
                // add verbiage
                strForBot += _rumor.AddBaitAction();
                var hasEarlyPicks = _rumor.CheckForFirstRounders(tradeBait.willGiveUp);
                var multiplePlayers = _rumor.CheckForMultiplePlayers(tradeBait.willGiveUp);
                if (multiplePlayers)
                {
                    var players = (await _mfl.GetBotPlayersDetails(tradeBait.willGiveUp)).players.player;
                    strForBot += _rumor.ListPlayers(players, hasEarlyPicks);
                }
                else
                {
                    var player = (await _mfl.GetBotPlayerDetails(tradeBait.willGiveUp)).players.player;
                    strForBot += _rumor.ListPlayer(player, hasEarlyPicks);
                }
                await BotPost(strForBot);
                return;
            }
            catch (Exception) {Console.WriteLine("not a single trade");}
            try
            {
                var tradeBaits = deserializer.Deserialize<TradeBaitsParent>(jsonString, res, info).tradeBaits.tradeBait;
                foreach (var post in tradeBaits)
                {
                    var postDate = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(post.timestamp));
                    if (postDate > DateTime.Now.AddMinutes(-11))
                    {
                        strForBot += _rumor.GetSources();
                        _owners.TryGetValue(Int32.Parse(post.franchise_id), out var ownerName);
                        strForBot += $"{ownerName} ";
                        strForBot += _rumor.AddBaitAction();  // add verbage
                        var hasEarlyPicks = _rumor.CheckForFirstRounders(post.willGiveUp);
                        if (_rumor.CheckForMultiplePlayers(post.willGiveUp))
                        {
                            var players = (await _mfl.GetBotPlayersDetails(post.willGiveUp)).players.player;
                            strForBot += _rumor.ListPlayers(players, hasEarlyPicks);
                        }
                        else
                        {
                            var player = (await _mfl.GetBotPlayerDetails(post.willGiveUp)).players.player;
                            strForBot += _rumor.ListPlayer(player, hasEarlyPicks);
                        }
                        await BotPost(strForBot);
                    }
                }
            }
            catch (Exception) { Console.WriteLine("not a multi trade");}
        }

        public async Task<string> FindAndPostContract(int year, string nameSearch)
        {
            // get list of players on rosters
            var rosters = (await _mfl.GetRostersWithContracts(year)).rosters.franchise;
            var rosteredPlayers = rosters.SelectMany(_ => _.player).ToList();
            // build query string with all player ids in that list 
            var queryIds = "";
            rosteredPlayers.ForEach(player => queryIds = $"{queryIds}{player.id},");

            // get player details for that query
            var playerDetails = (await _mfl.GetBotPlayersDetails(queryIds)).players.player;
            // match the list of players with the list of contracts
            rosteredPlayers.ForEach(player =>
            {
                var match = playerDetails.FirstOrDefault(p => p.id == player.id);
                if (match != null)
                    player.name = InvertNameString(match.name);
            });
            // search through with the name search to find players with that string in their name
            var hits = rosteredPlayers.Where(p => p.name.Contains(nameSearch)).ToList();
            hits.ForEach(p =>
            {
                var owner = rosters.FirstOrDefault(tm => tm.player.Any(_ => _.id == p.id));
                p.owner = owner == null ? "" : _owners.GetValueOrDefault(Int32.Parse(owner.id));
            });
            // bot post those names and contracts
            var stringForBot = "";
            if (!hits.Any())
                stringForBot = "I couldn't find any players by that name with contracts.";
            else
            {
                hits.ForEach(p =>
                {
                    stringForBot = $"{stringForBot}{p.name} - ${p.salary}/{p.contractYear} ({p.owner})\n";
                });
            }

            await BotPost(stringForBot);
            return stringForBot;
        }

        public async Task CheckLineupsForHoles()
        {
            var thisWeek = (await _mfl.GetMatchupSchedule()).schedule.weeklySchedule.First(_ =>
                _.matchup.All(gm => gm.franchise.Any(tm => tm.result == "T" && tm.score == null))).week;

            // task when all get injuries and byes get scores with that week num
            var scoresTask = _mfl.GetLiveScores(thisWeek);
            var projectionsTask = _mfl.GetProjections(thisWeek);
            var byesTask = _globalMflApi.GetByesForWeek(thisWeek);
            var injuriesTask = _globalMflApi.GetInjuries(thisWeek);
            var allPlayersTask = _mfl.GetAllMflPlayers();
            var groupTask = _gmApi.GetMemberIds();

            try
            {
                await Task.WhenAll(scoresTask, projectionsTask, injuriesTask, byesTask, allPlayersTask, groupTask);
            }
            catch (Exception)
            {
                await BotPost("There was an issue retrieving the data.");
                return;
            }
            
            var memberList = groupTask.Result.response.members;
            // go through starters for each team. make sure theres no OUT or BYE (could also check for other messages)

            var teams = scoresTask.Result.liveScoring.matchup
                .SelectMany(game => game.franchise)
                .ToList();
            var onlyStarters = teams.Select(tm => new LiveScoreFranchise
            {
                id = tm.id,
                players = new LiveScoringPlayers
                {
                    player = tm.players.player.Where(p => p.status.ToLower() == "starter").ToList()
                }
            }).ToList();
            var allPlayers = allPlayersTask.Result.players.player
                .Where(p => p.position == "WR" || p.position == "QB" || p.position == "TE" || p.position == "RB").ToList();
            var byesThisWeek = byesTask.Result.nflByeWeeks.team.Select(t => t.id).ToList();
            var playersWhoAreOut = injuriesTask.Result.injuries.injury
                .Where(p => p.status.ToLower() != "questionable" || p.status.ToLower() != "doubtful")
                .Select(_ => _.id).ToList();
            var projectedForZero =
                projectionsTask.Result.projectedScores.playerScore
                    .Where(p =>
                    { 
                        var success = Double.TryParse(p.score, out var score); 
                        return success ? score == 0.0 : true;
                    }).Select(_ => _.id).ToList();

            onlyStarters.ForEach(async t =>
            {
                var botStr = "";
                var hasBye = false;
                var isOut = false;
                var isZeroPoints = false;
                var bustedTeams = 0;
                foreach (var player in t.players.player)
                {
                    //if (player.status == "nonstarter") return;
                    player.nflTeam = allPlayers.FirstOrDefault(allPlayer => allPlayer.id == player.id)?.team;
                    if (byesThisWeek.Contains(player.nflTeam)) hasBye = true;
                    if (playersWhoAreOut.Contains(player.id)) isOut = true;
                    if (projectedForZero.Contains(player.id)) isZeroPoints = true;
                    if (hasBye || isOut || isZeroPoints)
                    {
                        var tagName = memberList.Find(m => m.user_id == _memberIds[Int32.Parse(t.id)]);
                        var tagString = $"@{tagName?.nickname}";
                        botStr = ", your lineup is invalid";
                        await BotPostWithTag(botStr, tagString, tagName?.user_id ?? "");
                        bustedTeams++;
                        return;
                    }
                    //TODO: see if not starting TEN!
                }
                if (bustedTeams == 0) await BotPost("Lineups are all straight, mate.");
            });
            //TODO: mark if tankin'?
        }

        public async Task PostCapSpace()
        {
            var botStr = "Current Cap Space (Next Year)\n";
            var salarySummaries = new List<FranchiseCapSummary>();
            //get total salaries this season + adjustments, subtract from team budget
            var salariesTask = _mfl.GetRostersWithContracts(_thisYear);
            var leagueTask = _mfl.GetFullLeagueDetails(_thisYear);
            await Task.WhenAll(salariesTask, leagueTask);

            var teamAdjustedCaps = leagueTask.Result.league.franchises.franchise.Select(tm => new
            {
                Id = int.Parse(tm.id),
                SalaryCapAmount = string.IsNullOrEmpty(tm.salaryCapAmount) ? 500 : int.Parse(tm.salaryCapAmount)
            });
            var salaries = salariesTask.Result.rosters.franchise;
            var adjustments = _leagueService.GetDeadCapData();
            // add up salaries for this year - but dont forget to * .5  and .4 for taxi and IR
            
            salaries.ForEach(roster =>
            {
                var rosterBoys = roster.player.Where(p => p.status == "ROSTER").Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                var irBoys = roster.player.Where(p => p.status == "INJURED_RESERVE").Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                var taxiBoys = roster.player.Where(p => p.status == "TAXI_SQUAD").Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                var nextYearBoys = roster.player.Where(p => Int32.Parse(p.contractYear) > 1).Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = _.status == "TAXI_SQUAD" ? (decimal)0.2 * Decimal.Parse(_.salary) : Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                salarySummaries.Add(new FranchiseCapSummary
                {
                    
                    Id = Int32.Parse(roster.id),
                    CurrentRosterSalary = rosterBoys.Sum(_ => _.Salary),
                    CurrentIRSalary = ((decimal) 0.5 * irBoys.Sum(_ => _.Salary)),
                    CurrentTaxiSalary = ((decimal) 0.2 * taxiBoys.Sum(_ => _.Salary)),
                    DeadCapData = adjustments.FirstOrDefault(_ => _.FranchiseId == Int32.Parse(roster.id))?.Amount,
                    NextYearRosterSalary = nextYearBoys.Sum(_ => _.Salary)
                });
            });
            var orderedSummaries = salarySummaries.OrderByDescending(tm =>
                tm.CurrentRosterSalary + tm.CurrentTaxiSalary + tm.CurrentIRSalary +
                (tm.DeadCapData.ContainsKey(_thisYear.ToString()) ? tm.DeadCapData[_thisYear.ToString()] : 0)).ToList();
            
            orderedSummaries.ForEach(tm =>
            {
                botStr += $"{_owners[tm.Id]}: " +
                          $"${teamAdjustedCaps.First(_ => _.Id == tm.Id).SalaryCapAmount - (tm.CurrentRosterSalary + tm.CurrentTaxiSalary + tm.CurrentIRSalary + (tm.DeadCapData.ContainsKey(_thisYear.ToString()) ? tm.DeadCapData[_thisYear.ToString()] : 0))} " +
                          $"(${500 - (tm.NextYearRosterSalary + (tm.DeadCapData.ContainsKey((_thisYear + 1).ToString()) ? tm.DeadCapData[(_thisYear + 1).ToString()] : 0))})\n";
            });
            await BotPost(botStr);
        }

        public async Task<string> FindAndPostLiveScores()
        {
            var thisWeek = (await _mfl.GetMatchupSchedule()).schedule.weeklySchedule.First(_ =>
                _.matchup.All(gm => gm.franchise.Any(tm => tm.result == "T" && tm.score == null))).week;
            var botText = "Live Scores (Live Projections)\n";
            var matchupScoresTask = _mfl.GetLiveScores(thisWeek);
            var scoreProjectionsTask = _mfl.GetProjections(thisWeek);
            await Task.WhenAll(new List<Task> {matchupScoresTask, scoreProjectionsTask});
            var matchups = matchupScoresTask.Result.liveScoring.matchup;
            var projections = scoreProjectionsTask.Result.projectedScores.playerScore;
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
            await BotPost(botText);
            return botText;
        }

        private string InvertNameString(string commaName)
        {
            if (string.IsNullOrEmpty(commaName)) return "";
            var nameArr = commaName.Split(",");
            return $"{nameArr[1]} {nameArr[0]}".ToLower();
        }

        public async Task PostHelpMessage()
        {
            var str = $"Check live scores with \"#scores\"\n" +
                      $"Check standings with \"#standings\"\n" +
                      $"Check player contract with \"#contract playername\"\n" +
                      $"Check if lineups are valid with \"#lineups\"\n" +
                      $"See team cap space with \"#cap\"";
            await BotPost(str);
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
            await BotPost($"If you need something from me, type \"#help\". {insultString}");
        }

        public async Task BotPost(string text)
        {
            var message = new Message(text);
            await _gmApi.SendMessage(message);
        }

        public async Task BotPostWithTag(string text, string nickname, string memberId)
        {
            var rawText = $"{nickname}{text}";
            var message = new Message(rawText);
            var mention = new Mention {type = "mentions"};
            int[][] locis = new int[1][] {new[] {0, nickname.Length}};
            var mentionIds = new[] {memberId};
            mention.loci = locis;
            mention.user_ids = mentionIds;
            var mentionList = new List<Mention> {mention};
            message.attachments = mentionList;
            await _gmApi.SendMessage(message);
        }
    }
}
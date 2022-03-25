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
        Task PostDraftProjections(int year);
        Task StrayTag();
        Task PostTopUpcomingFreeAgents(string positionRequested, int year = Utils.ThisYear);
        Task PostFranchiseTagAmounts(int year = Utils.ThisYear);
        Task PostFutureDeadCap();
    }
    
    public class GroupMeRequestRequestService : IGroupMeRequestService
    {
        private readonly IMflTranslationService _mflTranslationService;
        private readonly IDataSetHelperService _dataHelper;
        private IGroupMeApi _gmApi;
        private readonly IMflApi _mfl;
        private readonly IGlobalMflApi _globalMflApi;
        private readonly ILeagueService _leagueService;
        private readonly IRumorService _rumor;
        private readonly IInsultApi _insult;
        private static Dictionary<int, string> _owners;
        private static Dictionary<int, string> _memberIds;
        private static int _thisYear;
        
        public GroupMeRequestRequestService(IMflTranslationService mflTranslationService, IDataSetHelperService dataHelper, IGroupMeApi gmApi, IMflApi mfl, IGlobalMflApi globalMflApi, ILeagueService leagueService, IRumorService rumor, IInsultApi insult)
        {
            _mflTranslationService = mflTranslationService;
            _dataHelper = dataHelper;
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
            var memberList = (await _gmApi.GetMemberIds()).response.members;

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
            DateTime tenMinAgo = DateTime.Now.AddMinutes(-11);

            var tradeInfoList = await _mflTranslationService.GetCompletedTrades();
            //TODO: CHECK FOR NULL?? it would mean it could not serialize it as either type
            
            foreach (var trade in tradeInfoList)
            {
                if (trade != null && !string.IsNullOrEmpty(trade.franchise2))
                {
                    var strForBot = "";
                    var tradeTime = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(trade.timestamp));
                    // check if trade was not in the last 10 minutes to bail early
                    if (tradeTime <= tenMinAgo) continue;
                    strForBot = await _rumor.GetCompletedTradeString(trade);
                    await BotPost(strForBot);
                }
            }
        }

        public async Task PostTradeRumor()
        {
            // TODO: MAKE A SERVICE THAT only TALKS TO MFL API AND RETURNS a friendly model. so bait list or single returns the same obj with list.
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
                        await BotPost(strForBot);
                    }
                }
            }
        }

        public async Task<string> FindAndPostContract(int year, string nameSearch)
        {
            if (nameSearch.EndsWith(" ")) nameSearch = nameSearch.Trim();
            // get list of players on rosters
            var hits = await _mflTranslationService.GetRosteredPlayersByName(year, nameSearch);
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
            var thisWeek = await _mflTranslationService.GetThisLeagueWeek();

            // task when all get injuries and byes get scores with that week num
            var scoresTask = _mflTranslationService.GetLiveScoresForFranchises(thisWeek);
            var byesTask = _mflTranslationService.GetByesThisWeek(thisWeek);
            var injuriesTask = _mflTranslationService.GetInjurredPlayerIdsThisWeek(thisWeek);
            var allPlayersTask = _mflTranslationService.GetAllRelevantPlayers();
            var groupTask = _gmApi.GetMemberIds();

            try
            {
                await Task.WhenAll(scoresTask, injuriesTask, byesTask, allPlayersTask, groupTask);
            }
            catch (Exception)
            {
                await BotPost("There was an issue retrieving the data.");
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
            onlyStarters.ForEach(async t =>
            {
                var botStr = ", your lineup is invalid";
                foreach (var player in t.players.player)
                {
                    player.nflTeam = allPlayersTask.Result.FirstOrDefault(allPlayer => allPlayer.id == player.id)?.team;
                    var hasBye = byesTask.Result.Contains(player.nflTeam);
                    var isOut = injuriesTask.Result.Contains(player.id);
                    if (!hasBye && !isOut) continue;
                    if (brokenTeams.Contains(t.id)) continue;
                    var tagName = memberList.Find(m => m.user_id == _memberIds[Int32.Parse(t.id)]);
                    var tagString = $"@{tagName?.nickname}";
                    await BotPostWithTag(botStr, tagString, tagName?.user_id ?? "");
                    brokenTeams.Add(t.id);
                }
            });
            //TODO: mark if tankin'?
            if (!brokenTeams.Any()) await BotPost("Lineups are all straight, mate.");
        }

        public async Task PostCapSpace()
        {
            var botStr = "Current Cap Space (Next Year)\n";
           
            //get total salaries this season + adjustments, subtract from team budget
            var salariesTask = _mflTranslationService.GetFranchiseSalaries();
            var leagueTask = _mflTranslationService.GetTeamAdjustedSalaryCaps();
            await Task.WhenAll(salariesTask, leagueTask);
            
            var adjustments = _leagueService.GetDeadCapData();
            // add up salaries for this year - but dont forget to * .5  and .4 for taxi and IR
            
            var orderedSummaries = _dataHelper.CreateFranchiseCapSummaries(salariesTask.Result, adjustments);
            
            orderedSummaries.ForEach(tm =>
            {
                botStr += $"{_owners[tm.Id]}: " +
                          $"${leagueTask.Result.First(_ => _.Id == tm.Id).SalaryCapAmount - (tm.CurrentRosterSalary + tm.CurrentTaxiSalary + tm.CurrentIRSalary + (tm.DeadCapData.ContainsKey(_thisYear.ToString()) ? tm.DeadCapData[_thisYear.ToString()] : 0))} " +
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

        public async Task PostFranchiseTagAmounts(int year = Utils.ThisYear)
        {
            var salariesTask = _mfl.GetSalaries(year);
            var positionTask = _mfl.GetAllMflPlayers(year);
            await Task.WhenAll(salariesTask, positionTask);

            var positionIds = positionTask.Result.players.player.Where(p =>
                p.position == "WR" || p.position == "QB" || p.position == "RB" || p.position == "TE").ToList();
            var playerSalaries = salariesTask.Result.Salaries.LeagueUnit.Player.Select(_ => new
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

            var strForBot = "Average salary of top 6 at each position:\n";
            tagAmounts.ForEach(t =>
            {
                strForBot += $"{t.Position}: ${Decimal.Round(t.Salary)}\n";
            });

            await BotPost(strForBot);
        }

        public async Task PostTopUpcomingFreeAgents(string positionRequest, int year = Utils.ThisYear)
        {
            var pos = positionRequest.ToUpper().Trim();
            if (pos != "QB" && pos != "RB" && pos != "WR" && pos != "TE") return;

            var strForBot = $"Top Upcoming {pos} Free Agents\n";
            var avgPtsTask = _mfl.GetAveragePlayerScores(year);
            var salariesTask = _mfl.GetSalaries(year);
            var playerTask = _mfl.GetAllMflPlayers(year);
            await Task.WhenAll(avgPtsTask, playerTask, salariesTask);

            var playerInfos =
                playerTask.Result.players.player.Where(p =>
                    p.position == "QB" || p.position == "RB" || p.position == "WR" || p.position == "TE").ToList();
            var scores = avgPtsTask.Result.playerScores.playerScore;
            var relevantPlayers =
                salariesTask.Result.Salaries.LeagueUnit.Player.Where(_ => _.ContractYear == "1" && _.Salary != "");

            var topScores = relevantPlayers.Select(_ => new
            {
                Id = _.Id,
                Salary = _.Salary,
                Name = playerInfos.FirstOrDefault(p => p.id == _.Id)?.name,
                Position = playerInfos.FirstOrDefault(p => p.id == _.Id)?.position,
                Score = Decimal.TryParse(scores.FirstOrDefault(p => p.id == _.Id)?.score, out var x) ? x : 0
            }).OrderByDescending(_ => _.Score).ToList();

            topScores.Where(_ => _.Position == pos).Take(8).ToList().ForEach(p =>
            {
                strForBot += $"{p.Name} - {p.Score} PPG\n";
            });
            await BotPost(strForBot);
        }

        public async Task PostDraftProjections(int year)
        {
            //TODO: need to check if we are predraft in the offseason, just get this year's order from that 
            var standingsTask = _mflTranslationService.GetFranchiseStandings();
            var draftPicksTask = _mfl.GetFranchiseAssets();
            await Task.WhenAll(standingsTask, draftPicksTask);

            if (standingsTask.Result.All(tm => tm.h2hw == "0" && tm.h2hl == "0")) //preseason
            {
                var draftPicks =
                    _mflTranslationService.GetCurrentFranchiseDraftPicks(draftPicksTask.Result.assets.franchise)
                        .Where(pk => pk.Round == 1 || pk.Round == 2)
                        .OrderBy(pk => pk.Round).ThenBy(pk => pk.Pick).ToList();
                var botStr = "";
                draftPicks.ForEach(pick =>
                {
                    var pickNum = $"{pick.Round}.{pick.Pick.ToString("D2")}";
                    botStr += $"{pickNum} {_owners[pick.CurrentOwner]}\n";
                });
                await BotPost(botStr);
            }
            else
            {
                var draftPicks =
                    _mflTranslationService.GetFutureFranchiseDraftPicks(draftPicksTask.Result.assets.franchise);
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
                            .First(d => d.Year == year && d.Round == rd && d.OriginalOwner == origSlot).CurrentOwner;
                        botStr += $"{pickNum}) {_owners[currentPickOwner]}";
                        botStr += origSlot == currentPickOwner ? "\n" : $" (via {_owners[origSlot]})\n";


                        pickNum++;
                    });
                    await BotPost(botStr);
                }
            }
        }

        public async Task PostFutureDeadCap()
        {
            var deadCapInfo = _leagueService.GetDeadCapData();
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
            await BotPost(botStr);
        }


        public async Task PostHelpMessage()
        {
            var str = $"Check live scores with \"#scores\"\n" +
                      $"Check standings with \"#standings\"\n" +
                      $"Check player contract with \"#contract playername\"\n" +
                      $"Check if lineups are valid with \"#lineups\"\n" +
                      $"See upcoming free agents at a position with \"#freeagents qb/wr/rb/te\"\n" +
                      $"See projected rookie draft picks with \"#draft\"\n" +
                      $"See team cap space with \"#cap\"\n " +
                      $"See future dead cap with \"#dead\"\n " +
                      $"Get franchise tag projections with \"#tag\"";
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
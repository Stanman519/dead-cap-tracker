using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DeadCapTracker.Models.BotModels;
using System.Xml.Serialization;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Repositories;
using Microsoft.Extensions.Logging;
using RestEase;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Numerics;
using System.IO;
using System.Security.Cryptography;

namespace DeadCapTracker.Services
{
    public interface IMflTranslationService
    {
        Task GiveNewContractToPlayer(int leagueId, int mflPlayerId, int salary, int length);
        Task<List<TradeSingle>> GetCompletedTrades(int leagueId);
        Task<List<TradeBait>> GetNewTradeBait(int leagueId);
        Task<List<RosterPlayer>> GetRosteredPlayersByName(int leagueId, int year, string name);
        Task<string> GetThisLeagueWeek(int leagueId);
        Task<List<Player>> GetAllRelevantPlayers(int leagueId);
        Task<List<LiveScoreFranchise>> GetLiveScoresForFranchises(int leagueId, string thisWeek);
        Task<List<string>> GetByesThisWeek(string thisWeek);
        Task<List<string>> GetInjurredPlayerIdsThisWeek(string thisWeek);
        Task<List<FranchiseRoster>> GetFranchiseSalaries(int leagueId);
        Task<List<TeamAdjustedSalaryCap>> GetTeamAdjustedSalaryCaps(int leagueId);
        List<DraftPickTranslation> GetFutureFranchiseDraftPicks(List<MflAssetsFranchise> franchises);
        Task<List<MflFranchiseStandings>> GetFranchiseStandings(int leagueId);
        List<DraftPickTranslation> GetCurrentFranchiseDraftPicks(List<MflAssetsFranchise> franchises);
        Task<List<StandingsV2>> GetStandings(int leagueId, int year);
        Task<List<MflSalaryAdjustment>> GetSalaryAdjustments(int leagueId, int year);
        Task<List<MflPlayer>> GetAllSalaries(int leagueId, int year);
        Task BuildAndPostSalaryAdjustments(int leagueId, List<SalaryAdjustment> adjustments, int year);
        Task<List<MflTransaction>> GetMflTransactionsByType(int leagueId, int year, string type);
        Task<List<PendingTradeDTO>> FindPendingTrades(int leagueId, int year);
        Task<List<Player>> GetMultiMflPlayers(int leagueId, string playerIds);
        Task<List<FranchiseDTO>> GetAllFranchises(int leagueId);
        Task<List<MflPlayer>> GetPlayersOnLastYearOfContract(int leagueId);
        Task<List<MflPlayerProfile>> GetMultiMflPlayerDetails(string playerIds);
        Task<List<MflPlayer>> GetFreeAgents(int leagueId, int year);
        Task<List<Matchup>> GetLiveScoresForMatchups(int leagueId, string thisWeek);
        Task<List<ProjectedPlayerScore>> GetProjections(int leagueId, string thisWeek);
        Task<List<PlayerAvgScore>> GetAveragePlayerScores(int leagueId, int year);
        Task<List<MflAssetsFranchise>> GetFranchiseAssets(int leagueId);
        Task<List<DraftPickWithSlotValue>> GetDraftPicksAndContractValues(int leagueId);
        Task SetLineupForFranchise(int leagueId, string starterIds, string franchiseId, string botId);
        int GetDraftPickPrice(int round, int pick);
        Task CutPlayersWithExpiredContracts(int leagueId, string queryString, string franchiseId);
    }

    public class MflTranslationService : IMflTranslationService
    {
        // This service is responsible for making calls through mfl api
        // and parsing data to be more manageable/correct models for other services to use

        private readonly IMflApi _mfl;
        private readonly IGlobalMflApi _globalMflApi;
        private readonly IGroupMePostRepo _gm;
        private static Dictionary<int, Dictionary<int, string>> _owners;
        private static Dictionary<int, Dictionary<int, string>> _memberIds;
        private readonly DeadCapTrackerContext _db;

        public IMapper Mapper { get; }
        public ILogger<MflTranslationService> _logger { get; }

        public MflTranslationService(IMflApi mfl, IGlobalMflApi globalMflApi, IRumorService rumor, IMapper mapper, ILogger<MflTranslationService> logger, IGroupMePostRepo gm, DeadCapTrackerContext db)
        {
            _mfl = mfl;
            _globalMflApi = globalMflApi;
            Mapper = mapper;
            _logger = logger;
            _gm = gm;
            _owners = Utils.owners;
            _memberIds = Utils.memberIds;
            _db = db;
        }

        public async Task<List<TradeSingle>> GetCompletedTrades(int leagueId)
        {
            var year = DateTime.UtcNow.Year;
            try
            {
                return (await _mfl.GetRecentTrade(leagueId, year, Utils.ApiKeys[leagueId])).transactions.transaction;
            }
            catch (Exception e) {
                _logger.LogError(e, "MFL Request Error");
                return new List<TradeSingle>();
            }
        }
        public async Task CutPlayersWithExpiredContracts(int leagueId, string queryString, string franchiseId)
        {
            var thisYear = DateTime.UtcNow.Year;
            var res = await _mfl.DropPlayer(leagueId, queryString, franchiseId, thisYear);
        }
        public async Task<List<TradeBait>> GetNewTradeBait(int leagueId)
        {
            var year = DateTime.UtcNow.Year;
            try //multi
            {
                return (await _mfl.GetTradeBait(leagueId, year, Utils.ApiKeys[leagueId])).tradeBaits.tradeBait;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MFL Request Error");
                return new List<TradeBait>();
            }

        }

        public async Task<List<RosterPlayer>> GetRosteredPlayersByName(int leagueId, int year, string name)
        {
            var rosters = new List<FranchiseRoster>();
            try
            {
                rosters = (await _mfl.GetRostersWithContracts(leagueId, year)).rosters.franchise;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MFL Request Error");
                return null;
            }

            var rosteredPlayers = rosters.SelectMany(_ => _.player).ToList();
            // build query string with all player ids in that list 
            var queryIds = "";
            rosteredPlayers.ForEach(player => queryIds = $"{queryIds}{player.id},");

            // get player details for that query
            var playerDetails = new List<Player>();
            try
            {
                playerDetails = (await _mfl.GetBotPlayersDetails(leagueId, queryIds, year, Utils.ApiKeys[leagueId])).players.player;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MFL Request Error");
                return null;
            }

            // match the list of players with the list of contracts

            var withNames = rosteredPlayers.GroupJoin(playerDetails,
                rp => rp.id,
                pd => pd.id,
                (rp, pd) => new
                {
                    rp,
                    nameRows = pd.DefaultIfEmpty()
                }).SelectMany(p => p.nameRows.Select(n => new RosterPlayer
                {
                    contractYear = p.rp.contractYear,
                    id = p.rp.id,
                    name = InvertNameString(n?.name),
                    owner = p.rp.owner,
                    salary = p.rp.salary,
                    status = p.rp.status
                })).ToList();

            // search through with the name search to find players with that string in their name
            var hits = withNames.Where(p => p.name.Contains(name)).ToList();
            hits.ForEach(p =>
            {
                var owner = rosters.FirstOrDefault(tm => tm.player.Any(_ => _.id == p.id));
                p.owner = owner == null ? "" : _owners[leagueId].GetValueOrDefault(Int32.Parse(owner.id));
            });
            return hits;
        }

        public async Task<string> GetThisLeagueWeek(int leagueId)
        {
            var year = DateTime.UtcNow.Year;
            return (await _mfl.GetMatchupSchedule(leagueId, year, Utils.ApiKeys[leagueId])).schedule.weeklySchedule.First(_ =>
                _.matchup.All(gm => gm.franchise.Any(tm => tm.result == "T" && tm.score == null))).week;
        }



        public async Task<List<Player>> GetAllRelevantPlayers(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetAllMflPlayers(leagueId, year, Utils.ApiKeys[leagueId])).players.player
                    .Where(p => p.position == "WR" || p.position == "QB" || p.position == "TE" || p.position == "RB").ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<Player>();
            }
        }

        public async Task<List<LiveScoreFranchise>> GetLiveScoresForFranchises(int leagueId, string thisWeek)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetLiveScores(leagueId, thisWeek, year)).liveScoring.matchup
                    .SelectMany(game => game.franchise)
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<Matchup>> GetLiveScoresForMatchups(int leagueId, string thisWeek)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetLiveScores(leagueId, thisWeek, year)).liveScoring.matchup
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<ProjectedPlayerScore>> GetProjections(int leagueId, string thisWeek)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetProjections(leagueId, thisWeek, year, Utils.ApiKeys[leagueId])).projectedScores.playerScore;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }
        }

        public async Task<List<string>> GetByesThisWeek(string thisWeek)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _globalMflApi.GetByesForWeek(thisWeek, year)).nflByeWeeks.team?
                    .Select(t => t.id).ToList() ?? new List<string>();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<string>> GetInjurredPlayerIdsThisWeek(string thisWeek)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _globalMflApi.GetInjuries(thisWeek, year)).injuries.injury
                    .Where(p => p.status.ToLower() != "questionable" && p.status.ToLower() != "doubtful")
                    .Select(_ => _.id).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<FranchiseRoster>> GetFranchiseSalaries(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetRostersWithContracts(leagueId, year)).rosters.franchise;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<TeamAdjustedSalaryCap>> GetTeamAdjustedSalaryCaps(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetFullLeagueDetails(leagueId, year)).league.franchises.franchise
                    .Select(tm => new TeamAdjustedSalaryCap()
                    {
                        Id = int.Parse(tm.id),
                        SalaryCapAmount = string.IsNullOrEmpty(tm.salaryCapAmount) ? 500 : decimal.Parse(tm.salaryCapAmount)
                    }).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<StandingsV2>> GetStandings(int leagueId, int year)
        {
            var modYear = year % 3;
            var yearArr = GetThreeYearArray(modYear, year);
            var dict = new Dictionary<int, MflStandingsParent>();
            var apiTasks = yearArr.Select(y => _mfl.GetStandings(leagueId, y, Utils.ApiKeys[leagueId]));
            var mflStandings = new List<MflStandingsParent>();
            try
            {
                mflStandings = (await Task.WhenAll(apiTasks)).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<StandingsV2>();
            }


            for (int i = 0; i < yearArr.Count; i++)
            {
                dict[yearArr[i]] = mflStandings[i];
            }

            var ret = new List<StandingsV2>();

            foreach (KeyValuePair<int, MflStandingsParent> standings in dict)
            {
                var aYearOfScoringData = standings.Value.LeagueStandings.Franchise.Select(f => new AnnualScoringData
                {
                    FranchiseId = Int32.Parse(f.id),
                    Year = standings.Key,
                    PointsFor = Decimal.Parse(f.pf),
                    H2hWins = Int32.Parse(f.h2hw),
                    H2hLosses = Int32.Parse(f.h2hl),
                    VictoryPoints = Int32.Parse(f.vp ?? "0")
                });
                foreach (var tm in aYearOfScoringData)
                {
                    if (!ret.Any(i => i.FranchiseId == tm.FranchiseId))
                    {
                        ret.Add(new StandingsV2
                        {
                            FranchiseId = tm.FranchiseId,
                            TeamStandings = new List<AnnualScoringData>()
                        });
                    }
                    var foundTeam = ret.First(i => i.FranchiseId == tm.FranchiseId);
                    foundTeam.TeamStandings.Add(tm);
                }
            }
            return ret;
        }

        public List<DraftPickTranslation> GetFutureFranchiseDraftPicks(List<MflAssetsFranchise> franchises)
        {
            var franchisePicks = franchises.Select(_ => new
            {
                FuturePicks = _.futureYearDraftPicks.draftPick.Select(x =>
                {
                    var arr = x.pick.Split("_");
                    return new DraftPickTranslation
                    {
                        Year = Int32.Parse(arr[2]),
                        Round = Int32.Parse(arr[3]),
                        OriginalOwner = Int32.Parse(arr[1]),
                        CurrentOwner = Int32.Parse(_.id)
                    };
                })
            }).ToList();
            return franchisePicks.SelectMany(_ => _.FuturePicks).ToList();
        }

        public List<DraftPickTranslation> GetCurrentFranchiseDraftPicks(List<MflAssetsFranchise> franchises)
        {
            var franchisePicks = franchises.SelectMany(_ => _?.currentYearDraftPicks?.draftPick?.Select(pick =>
                {
                    var arr = pick.pick.Split("_");
                    return new DraftPickTranslation
                    {
                        Year = DateTime.Now.Year,
                        Round = Int32.Parse(arr[1]) + 1,
                        Pick = Int32.Parse(arr[2]) + 1,
                        CurrentOwner = Int32.Parse(_.id),
                        SlotCost = GetDraftPickPrice(Int32.Parse(arr[1]) + 1, Int32.Parse(arr[2]) + 1)

                    };
                })
            ).ToList();

            return franchisePicks;
        }

        public async Task<List<DraftPickWithSlotValue>> GetDraftPicksAndContractValues(int leagueId)
        {
            var year = DateTime.UtcNow.Year;
            var picksMadeWithOutSalaries = new List<RookieDraftPick>();
            try
            {
                var mflDraftRoot = await _mfl.GetMflDraftResults(leagueId, year, Utils.ApiKeys[leagueId]);
                picksMadeWithOutSalaries = mflDraftRoot.DraftResults.DraftUnit.DraftPick.Where(p => !string.IsNullOrEmpty(p.Player)).ToList();

            } catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<DraftPickWithSlotValue>();
            }


            var queryString = string.Join(",", picksMadeWithOutSalaries.Select(p => p.Player));

            var picksWithPlayerInfo = await _mfl.GetBotPlayersDetails(leagueId, queryString, year, Utils.ApiKeys[leagueId]);

            var picksWithValues = picksMadeWithOutSalaries.Select(_ => {
                var rawSalary = GetDraftPickPrice(int.Parse(_.Round), int.Parse(_.Pick));
                var playerInfo = picksWithPlayerInfo.players.player.FirstOrDefault(p => p.id == _.Player);
                var fixedSalary = 0.0;
                if (playerInfo != null) fixedSalary = playerInfo.position == "RB" ? rawSalary * 1.2 : rawSalary * 1.0;
                return new DraftPickWithSlotValue
                {
                    Player = _.Player,
                    Pick = _.Pick,
                    Round = _.Round,
                    Franchise = _.Franchise,
                    Timestamp = _.Timestamp,
                    Salary = (int)Math.Round(fixedSalary),
                    Length = int.Parse(_.Round) > 2 ? 3 : 4
                };
            }).ToList();

            return picksWithValues;
        }

        public int GetDraftPickPrice(int round, int pick)
        {
            var slot = (round - 1) * 12 + pick;
            if (slot > 36) slot = 37;
            return Utils.draftPicks[slot];

        }

        public async Task<List<MflFranchiseStandings>> GetFranchiseStandings(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetStandings(leagueId, year, Utils.ApiKeys[leagueId])).LeagueStandings.Franchise
                    .OrderBy(tm => Int32.Parse(tm.vp))
                    .ThenBy(tm => int.Parse(tm.h2hw))
                    .ThenBy(tm => Decimal.Parse(tm.pf)).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflFranchiseStandings>();
            }

        }

        public async Task<List<MflSalaryAdjustment>> GetSalaryAdjustments(int leagueId, int year)
        {
            try
            {
                return (await _mfl.GetSalaryAdjustments(leagueId, year)).salaryAdjustments.salaryAdjustment;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflSalaryAdjustment>();
            }
        }

        public async Task<List<MflTransaction>> GetMflTransactionsByType(int leagueId, int year, string type = "")
        {
            try
            {
                var ret = (await _mfl.GetMflTransactions(leagueId, year)).transactions.transaction;
                if (!string.IsNullOrEmpty(type))
                {
                    return ret.Where(t => t.type == type).ToList();
                }
                return ret;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflTransaction>();
            }



        }

        public async Task<List<PendingTradeDTO>> FindPendingTrades(int leagueId, int year)
        {
            var DTOs = new List<PendingTradeDTO>();
            var mflTasks = new List<Task<MflPendingTradesListRoot>>();
            for (int i = 2; i < 13; i++)
            {
                string franchiseNum = i.ToString("D4");
                mflTasks.Add(_mfl.GetPendingTrades(leagueId, franchiseNum, year, Utils.ApiKeys[leagueId]));

            }
            var dbTradesTask = _db.Proposals.Where(p => p.MflTradeId == null).ToListAsync();
            var allTasks = mflTasks.Cast<Task>().ToList();  // Cast MFL tasks to Task type
            try
            {

                allTasks.Add(dbTradesTask);
                await Task.WhenAll(mflTasks);
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<PendingTradeDTO>();
            }

            //look for trades in db that dont have a tradeId.  do it based on offering team & expires.
            var flattenedMflProposals = mflTasks
                .Select(_ => _.Result)
                .Where(result => result?.pendingTrades?.pendingTrade != null)
                .SelectMany(result => result.pendingTrades.pendingTrade)
                .ToList();
            dbTradesTask.Result.ForEach(t =>
            {
                var fixMe = flattenedMflProposals.FirstOrDefault(p => p.offeringTeam == t.SenderId.ToString("D4") && p.expires == t.Expires.ToString());
                if (fixMe != null)
                {
                    t.MflTradeId = int.Parse(fixMe.trade_id);
                }
            });
            await _db.SaveChangesAsync();

            foreach (var task in mflTasks)
            {
                var response = task.Result;
                var multiTrades = Mapper.Map<List<MflPendingTrade>, List<PendingTradeDTO>>(response.pendingTrades.pendingTrade);
                DTOs.AddRange(multiTrades);
            }
            //select only unique trade ids
            return DTOs.GroupBy(t => t.tradeId).Select(t => t.First()).ToList();
        }

        public async Task<List<Player>> GetMultiMflPlayers(int leagueId, string playerIds)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                return (await _mfl.GetBotPlayersDetails(leagueId, playerIds, year, Utils.ApiKeys[leagueId])).players.player;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<Player>();
            }

        }

        public async Task<List<FranchiseDTO>> GetAllFranchises(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                var leagueInfo = await _mfl.GetLeagueInfo(leagueId, year);
                var allFranchises = leagueInfo.League.Franchises.Franchise;
                return Mapper.Map<List<MflFranchise>, List<FranchiseDTO>>(allFranchises);
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<FranchiseDTO>();
            }

        }

        public async Task<List<MflPlayer>> GetPlayersOnLastYearOfContract(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                var salaries = await _mfl.GetSalaries(leagueId, year, Utils.ApiKeys[leagueId]);
                return salaries.Salaries.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayer>();
            }

        }

        public async Task<List<MflPlayer>> GetAllSalaries(int leagueId, int year)
        {
            try
            {
                var salaries = await _mfl.GetSalaries(leagueId, year, Utils.ApiKeys[leagueId]);
                return salaries.Salaries.LeagueUnit.Player;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayer>();
            }

        }

        public async Task<List<MflAssetsFranchise>> GetFranchiseAssets(int leagueId)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                var res = await _mfl.GetFranchiseAssets(leagueId, year, Utils.ApiKeys[leagueId]);
                return res.assets.franchise;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflAssetsFranchise>();
            }

        }

        public async Task<List<MflPlayerProfile>> GetMultiMflPlayerDetails(string playerIds)
        {
            try
            {
                var year = DateTime.UtcNow.Year;
                var playerDetails = await _globalMflApi.GetPlayerDetails(playerIds, year);
                return playerDetails.playerProfiles.playerProfile.ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayerProfile>();
            }

        }
        public async Task<List<MflPlayer>> GetFreeAgents(int leagueId, int year)
        {
            try
            {
                var rawMfl = await _mfl.GetFreeAgents(leagueId, year, Utils.ApiKeys[leagueId]);
                return rawMfl.freeAgents.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayer>();
            }

        }
        public async Task<List<PlayerAvgScore>> GetAveragePlayerScores(int leagueId, int year)
        {
            try
            {
                return (await _mfl.GetAveragePlayerScores(leagueId, year, Utils.ApiKeys[leagueId])).playerScores.playerScore;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<PlayerAvgScore>();
            }

        }
        public async Task GiveNewContractToPlayer(int leagueId, int mflPlayerId, int salary, int length)
        {
            if (!Utils.leagueBotDict.TryGetValue(leagueId, out var botId)) {
                _gm.BotPost(string.Empty, "giveContractToNewPlayerfailed with bad league id...", true);
                return;
            };
            var year = DateTime.UtcNow.Year;
            var data = CreateBodyDataForNewContract(mflPlayerId, salary, length);
            try
            {
                var resp = await _mfl.EditPlayerSalary(leagueId, data, year);
                var respString = await resp.Content.ReadAsStringAsync();
                if (respString.ToUpper().Contains("ERROR"))
                {
                    var error = respString.XmlDeserializeFromString<MflXmlError>();
                    _logger.LogInformation(respString);
                    _logger.LogError("{lastname}'s contract was not updated in mfl.", mflPlayerId);
                    await _gm.BotPost(botId, $"league: {leagueId} player:{mflPlayerId} contract was not updated in mfl. \n\n${error.ErrorMsg}", isError: true);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "New Contract mfl");
                return;
            }
        }

        public async Task SetLineupForFranchise(int leagueId, string starterIds, string franchiseId, string botId)
        {
            var year = DateTime.UtcNow.Year;
            try
            {
                var resp = await _mfl.SetLineupForFranchiseId(leagueId, year, franchiseId, starterIds);
                var respString = await resp.Content.ReadAsStringAsync();
                if (respString.ToUpper().Contains("ERROR"))
                {
                    var error = respString.XmlDeserializeFromString<MflXmlError>();
                    _logger.LogInformation(respString);
                    _logger.LogError("{franchiseId} lineup failed to submit", franchiseId);
                    throw new Exception($"lineup failed to submit to mfl.  \nresponse: \n{respString}");
                    
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "lineup submission");
                throw;
            }
        }


        public async Task BuildAndPostSalaryAdjustments(int leagueId, List<SalaryAdjustment> adjustments, int year)
        {
            //TODO: CHECK HERE TO MAKE SURE THESE SALARY ADJUSTMENTS ARENT ALREADY IN MFL
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
            

            // we have timestamp, so just check that theres not another one with the same amount and franchise within the last 15? that starts with SENDING/RECEIVING?
            var adjResp = await _mfl.GetSalaryAdjustments(leagueId, year);
            var existingAdjustments = adjResp.salaryAdjustments.salaryAdjustment.Where(old => long.Parse(old.Timestamp) > yesterday);
            var preExistingConditions = adjustments.Join(existingAdjustments, 
                n => new { franchise = n.franchiseId, amount = n.adjustmentAmount, firstWord = n.reason.Split(" ")[0], lastName = n.player.LastName }, 
                o => new { franchise = o.Franchise_Id, amount = double.Parse(o.Amount), firstWord = o.Description.Split(" ")[0], lastName = string.Join(" ", o.Description.Split(",")[0].Split(" ").Skip(1)) }, 
                (n, o) =>  new SalaryAdjustment
                    {
                        adjustmentAmount = n.adjustmentAmount,
                        franchiseId = n.franchiseId,
                        reason = n.reason,
                        length = n.length,
                        player = n.player
                    }
                ).ToList();
            if (preExistingConditions.Count() > 0)
            {
                if (preExistingConditions.Count() != adjustments.Count)
                {
                    await _gm.BotPost("", $"There's already some cap eats here for the {leagueId} trade that has {preExistingConditions.First().player.LastName}", true);
                }
                
                return;  
            }
            try
            {
                var body = CreateBodyDataForNewSalaryAdj(adjustments);
                var resp = await _mfl.AddSalaryAdjustment(leagueId, body, year);
                var respString = await resp.Content.ReadAsStringAsync();
                if (respString.ToUpper().Contains("ERROR"))
                {
                    var error = respString.XmlDeserializeFromString<MflXmlError>();
                    _logger.LogInformation(error.ErrorMsg);
                    await _gm.BotPost("", $"{leagueId} trade failed on salary adjustments! \n\n{error.ErrorMsg}", true);
                }
            }
            catch (Exception e) 
            {
                await _gm.BotPost("", $"{leagueId} trade exception on salary adjustments! \n\n{e.Message}", true);
            }

            return;
        }

        private Dictionary<string, string> CreateBodyDataForNewSalaryAdj(List<SalaryAdjustment> salAdjustments)
        {
            StringBuilder sb = new StringBuilder();

            salAdjustments.ForEach(s =>
            {
                sb.Append($"<salary_adjustment franchise_id =\"{s.franchiseId}\" amount=\"{s.adjustmentAmount}\" explanation=\"{s.reason} {s.player.LastName}, {s.player.FirstName} {s.player.Team} {s.player.Position} (Salary: ${s.player.Salary}, years left: {s.length})\"/>");
            });
            var salaryAdjustments = sb.ToString();

            var ret = new Dictionary<string, string>()
            {
                {
                    "DATA",
                    $"<?xml version='1.0' encoding='UTF-8' ?><salary_adjustments>{salaryAdjustments}</salary_adjustments>"
                }
            };
            return ret;
        }
        private Dictionary<string, string> CreateBodyDataForNewContract(int playerId, int salary, int length = 1)
        {
            var ret = new Dictionary<string, string>()
            {
                {
                    "DATA",
                    $"<?xml version='1.0' encoding='UTF-8' ?><salaries><leagueUnit unit=\"LEAGUE\"><player id=\"{playerId}\" salary=\"{salary}\" contractYear=\"{length}\"/></leagueUnit></salaries>"
                }
            };
            return ret;
        }

        public string InvertNameString(string commaName)
        {
            if (string.IsNullOrEmpty(commaName)) return "";
            var nameArr = commaName.Split(",");
            return $"{nameArr[1]} {nameArr[0]}".ToLower();
        }

        private List<int> GetThreeYearArray(int modYear, int year)
        {
            var yearArr = new List<int>();
            //year 1 is a 1
            //year 2 is a 2
            //year 3 is a 0

            if (modYear == 1)
            {
                yearArr.Add(year);
            }
            else if (modYear == 2)
            {
                yearArr.Add(year - 1);
                yearArr.Add(year);
            }
            else
            {
                yearArr.Add(year - 2);
                yearArr.Add(year - 1);
                yearArr.Add(year);
            }
            return yearArr;
        }

    }
    public class SalaryAdjustment
    {
        public string franchiseId { get; set; }
        public double adjustmentAmount { get; set; }
        public string reason { get; set; }
        public PlayerDTO player { get; set; }
        public int length { get; set; }
    }

}
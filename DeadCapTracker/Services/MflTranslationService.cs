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

namespace DeadCapTracker.Services
{
    public interface IMflTranslationService
    {
        Task GiveNewContractToPlayer(int leagueId, int mflPlayerId, int salary, int length);
        Task<List<TradeSingle>> GetCompletedTrades();
        Task<List<TradeBait>> GetNewTradeBait();
        Task<List<RosterPlayer>> GetRosteredPlayersByName(int year, string name);
        Task<string> GetThisLeagueWeek();
        Task<List<Player>> GetAllRelevantPlayers();
        Task<List<LiveScoreFranchise>> GetLiveScoresForFranchises(string thisWeek);
        Task<List<string>> GetByesThisWeek(string thisWeek);
        Task<List<string>> GetInjurredPlayerIdsThisWeek(string thisWeek);
        Task<List<FranchiseRoster>> GetFranchiseSalaries();
        Task<List<TeamAdjustedSalaryCap>> GetTeamAdjustedSalaryCaps();
        List<DraftPickTranslation> GetFutureFranchiseDraftPicks(List<MflAssetsFranchise> franchises);
        Task<List<MflFranchiseStandings>> GetFranchiseStandings();
        List<DraftPickTranslation> GetCurrentFranchiseDraftPicks(List<MflAssetsFranchise> franchises);
        Task<List<StandingsV2>> GetStandings(int year);
        Task<List<MflSalaryAdjustment>> GetSalaryAdjustments(int leagueId, int year);
        Task<List<MflPlayer>> GetAllSalaries(int year = Utils.ThisYear);
        Task<List<MflTransaction>> GetMflTransactionsByType(int leagueId, int year, string type);
        Task<List<PendingTradeDTO>> FindPendingTrades(int year);
        Task<List<Player>> GetMultiMflPlayers(string playerIds);
        Task<List<FranchiseDTO>> GetAllFranchises();
        Task<List<MflPlayer>> GetPlayersOnLastYearOfContract();
        Task<List<MflPlayerProfile>> GetMultiMflPlayerDetails(string playerIds);
        Task<List<MflPlayer>> GetFreeAgents(int year);
        Task<List<Matchup>> GetLiveScoresForMatchups(string thisWeek);
        Task<List<ProjectedPlayerScore>> GetProjections(string thisWeek);
        Task<List<PlayerAvgScore>> GetAveragePlayerScores(int year);
        Task<List<MflAssetsFranchise>> GetFranchiseAssets();
        Task<List<DraftPickWithSlotValue>> GetDraftPicksAndContractValues(int leagueId);
        int GetDraftPickPrice(int round, int pick);
    }

    public class MflTranslationService : IMflTranslationService
    {
        // This service is responsible for making calls through mfl api
        // and parsing data to be more manageable/correct models for other services to use

        private readonly IMflApi _mfl;
        private readonly IGlobalMflApi _globalMflApi;
        private readonly IGroupMePostRepo _gm;
        private static Dictionary<int, string> _owners;
        private static Dictionary<int, string> _memberIds;
        private static int _thisYear;
        

        public IMapper Mapper { get; }
        public ILogger<MflTranslationService> _logger { get; }

        public MflTranslationService(IMflApi mfl, IGlobalMflApi globalMflApi, IRumorService rumor, IMapper mapper, ILogger<MflTranslationService> logger, IGroupMePostRepo gm)
        {
            _mfl = mfl;
            _globalMflApi = globalMflApi;
            Mapper = mapper;
            _logger = logger;
            _gm = gm;
            _owners = Utils.owners;
            _memberIds = Utils.memberIds;
            _thisYear = Utils.ThisYear;
        }

        public async Task<List<TradeSingle>> GetCompletedTrades()
        {
            try 
            {
                return (await _mfl.GetRecentTrade()).transactions.transaction;
            }
            catch (Exception e) {
                _logger.LogError(e, "MFL Request Error");
                return new List<TradeSingle>(); 
            }
        }

        public async Task<List<TradeBait>> GetNewTradeBait()
        {
            // TODO: maybe instead of this process, just post a message saying there has been new trade bait and that you can message to see what it is
            try //multi
            {
                return (await _mfl.GetTradeBait()).tradeBaits.tradeBait;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MFL Request Error");
                return new List<TradeBait>();
            }

        }

        public async Task<List<RosterPlayer>> GetRosteredPlayersByName(int year, string name)
        {
            var rosters = new List<FranchiseRoster>();
            try
            {
                rosters = (await _mfl.GetRostersWithContracts(year)).rosters.franchise;
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
                playerDetails = (await _mfl.GetBotPlayersDetails(queryIds)).players.player;
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
                p.owner = owner == null ? "" : _owners.GetValueOrDefault(Int32.Parse(owner.id));
            });
            return hits;
        }

        public async Task<string> GetThisLeagueWeek()
        {
            return (await _mfl.GetMatchupSchedule()).schedule.weeklySchedule.First(_ =>
                _.matchup.All(gm => gm.franchise.Any(tm => tm.result == "T" && tm.score == null))).week;
        }



        public async Task<List<Player>> GetAllRelevantPlayers()
        {
            try
            {
                return (await _mfl.GetAllMflPlayers()).players.player
                    .Where(p => p.position == "WR" || p.position == "QB" || p.position == "TE" || p.position == "RB").ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<Player>();
            }
        }

        public async Task<List<LiveScoreFranchise>> GetLiveScoresForFranchises(string thisWeek)
        {
            try
            {
                return (await _mfl.GetLiveScores(thisWeek)).liveScoring.matchup
                    .SelectMany(game => game.franchise)
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<Matchup>> GetLiveScoresForMatchups(string thisWeek)
        {
            try
            {
                return (await _mfl.GetLiveScores(thisWeek)).liveScoring.matchup
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<ProjectedPlayerScore>> GetProjections(string thisWeek)
        {
            try
            {
                return (await _mfl.GetProjections(thisWeek)).projectedScores.playerScore;
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
                return (await _globalMflApi.GetByesForWeek(thisWeek)).nflByeWeeks.team?
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
                return (await _globalMflApi.GetInjuries(thisWeek)).injuries.injury
                    .Where(p => p.status.ToLower() != "questionable" && p.status.ToLower() != "doubtful")
                    .Select(_ => _.id).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<FranchiseRoster>> GetFranchiseSalaries()
        {
            try
            {
                return (await _mfl.GetRostersWithContracts(_thisYear)).rosters.franchise;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                throw;
            }

        }

        public async Task<List<TeamAdjustedSalaryCap>> GetTeamAdjustedSalaryCaps()
        {
            try
            {
                return (await _mfl.GetFullLeagueDetails(_thisYear)).league.franchises.franchise
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

        public async Task<List<StandingsV2>> GetStandings(int year)
        {
            var modYear = year % 3;
            var yearArr = GetThreeYearArray(modYear, year);
            var dict = new Dictionary<int, MflStandingsParent>();
            var apiTasks = yearArr.Select(y => _mfl.GetStandings(y));
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
                        Year = _thisYear,
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
            var mflDraftRoot = await _mfl.GetMflDraftResults(leagueid: leagueId);
            var picksMadeWithOutSalaries = mflDraftRoot.DraftResults.DraftUnit.DraftPick.Where(p => !string.IsNullOrEmpty(p.Player));
            var picksWithValues = picksMadeWithOutSalaries.Select(_ => new DraftPickWithSlotValue
            {
                Player = _.Player,
                Pick = _.Pick,
                Round = _.Round,
                Franchise = _.Franchise,
                Timestamp = _.Timestamp,
                Salary = GetDraftPickPrice(int.Parse(_.Round), int.Parse(_.Pick)),
                Length = int.Parse(_.Round) > 2 ? 3 : 4
            }).ToList();
            return picksWithValues;
        }

        public int GetDraftPickPrice(int round, int pick)
        {   
            var slot = (round - 1) * 12 + pick;
            if (slot > 36) slot = 37;
            return Utils.draftPicks[slot];

        }

        public async Task<List<MflFranchiseStandings>> GetFranchiseStandings()
        {
            try
            {
                return (await _mfl.GetStandings(_thisYear)).LeagueStandings.Franchise
                    .OrderBy(tm => Int32.Parse(tm.vp))
                    .ThenBy(tm => int.Parse(tm.h2hw))
                    .ThenBy(tm => Decimal.Parse(tm.pf)).ToList();
            }
            catch (Exception e )
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
        
        public async Task<List<PendingTradeDTO>> FindPendingTrades(int year)
        {
            var DTOs = new List<PendingTradeDTO>();
            var responses = new List<Task<MflPendingTradesListRoot>>();
            for (int i = 2; i < 13; i++)
            {
                string franchiseNum = i.ToString("D4");
                responses.Add(_mfl.GetPendingTrades(franchiseNum));
            }
            try
            {
                await Task.WhenAll(responses);
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<PendingTradeDTO>();
            }


            foreach (var task in responses)
            {
                var response = task.Result;
                var multiTrades = Mapper.Map<List<MflPendingTrade>, List<PendingTradeDTO>>(response.pendingTrades.pendingTrade);
                DTOs.AddRange(multiTrades);
            }
            //select only unique trade ids
            return DTOs.GroupBy(t => t.tradeId).Select(t => t.First()).ToList();
        }

        public async Task<List<Player>> GetMultiMflPlayers(string playerIds)
        {
            try
            {
                return (await _mfl.GetBotPlayersDetails(playerIds)).players.player;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<Player>();
            }

        }

        public async Task<List<FranchiseDTO>> GetAllFranchises()
        {
            try
            {
                var leagueInfo = await _mfl.GetLeagueInfo();
                var allFranchises = leagueInfo.League.Franchises.Franchise;
                return Mapper.Map<List<MflFranchise>, List<FranchiseDTO>>(allFranchises);
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<FranchiseDTO>();
            }

        }

        public async Task<List<MflPlayer>> GetPlayersOnLastYearOfContract()
        {
            try
            {
                var salaries = await _mfl.GetSalaries();
                return salaries.Salaries.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayer>();
            }

        }

        public async Task<List<MflPlayer>> GetAllSalaries(int year = Utils.ThisYear)
        {
            try
            {
                var salaries = await _mfl.GetSalaries(year);
                return salaries.Salaries.LeagueUnit.Player;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayer>();
            }

        }

        public async Task<List<MflAssetsFranchise>> GetFranchiseAssets()
        {
            try
            {
                return (await _mfl.GetFranchiseAssets()).assets.franchise;
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
                var playerDetails = await _globalMflApi.GetPlayerDetails(playerIds);
                return playerDetails.playerProfiles.playerProfile.ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayerProfile>();
            }

        }
        public async Task<List<MflPlayer>> GetFreeAgents(int year)
        {
            try
            {
                var rawMfl = await _mfl.GetFreeAgents(year);
                return rawMfl.freeAgents.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<MflPlayer>();
            }

        }
        public async Task<List<PlayerAvgScore>> GetAveragePlayerScores(int year)
        {
            try
            {
                return (await _mfl.GetAveragePlayerScores(year)).playerScores.playerScore;
            }
            catch (Exception e)
            {
                _logger.LogError("mfl error", e);
                return new List<PlayerAvgScore>();
            }

        }
        public async Task GiveNewContractToPlayer(int leagueId, int mflPlayerId, int salary, int length)
        {
            var data = CreateBodyDataForNewContract(mflPlayerId, salary, length);
            try
            {
                var resp = await _mfl.EditPlayerSalary(leagueId, data);
                var respString = await resp.Content.ReadAsStringAsync();
                if (respString.ToUpper().Contains("ERROR"))
                {
                    var error = respString.XmlDeserializeFromString<MflXmlError>();
                    _logger.LogInformation(respString);
                    _logger.LogError("{lastname}'s contract was not updated in mfl.", mflPlayerId);
                    await _gm.BotPost($"league: {leagueId} player:{mflPlayerId} contract was not updated in mfl. \n\n${error.ErrorMsg}", isError: true) ;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "New Contract mfl");
                return;
            }
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
}
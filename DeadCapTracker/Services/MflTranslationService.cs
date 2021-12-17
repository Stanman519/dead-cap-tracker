using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Repositories;
using RestEase;

namespace DeadCapTracker.Services
{
    public interface IMflTranslationService
    {
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
        Task<List<DraftPickTranslation>> GetFranchiseDraftPicks();
        Task<List<MflFranchiseStandings>> GetFranchiseStandings();
    }

    public class MflTranslationService : IMflTranslationService
    {
        // This service is responsible for making calls through mfl api
        // and parsing data to be more manageable/correct models for other services to use

        private readonly IMflApi _mfl;
        private readonly IGlobalMflApi _globalMflApi;
        private static Dictionary<int, string> _owners;
        private static Dictionary<int, string> _memberIds;
        private static int _thisYear;


        public MflTranslationService(IMflApi mfl, IGlobalMflApi globalMflApi, IRumorService rumor)
        {
            _mfl = mfl;
            _globalMflApi = globalMflApi;
            _owners = Utils.owners;
            _memberIds = Utils.memberIds;
            _thisYear = Utils.ThisYear;
        }

        public async Task<List<TradeSingle>> GetCompletedTrades()
        {
            var deserializer = new JsonResponseDeserializer();
            var info = new ResponseDeserializerInfo();
            var tradeRes = await _mfl.GetRecentTrade();
            var jsonString = await tradeRes.Content.ReadAsStringAsync();

            try //Single
            {
                var tradeSingle = deserializer.Deserialize<TradeTransactionSingle>(jsonString, tradeRes, info)
                    .transactions.transaction;
                return new List<TradeSingle> {tradeSingle};
            }
            catch (Exception) { Console.WriteLine("not a single trade"); }

            try //multi
            {
                return deserializer.Deserialize<TradeTransactionMulti>(jsonString, tradeRes, info)
                    .transactions.transaction;
            }
            catch (Exception) { return null; }
        }

        public async Task<List<TradeBait>> GetNewTradeBait()
        {
            // TODO: maybe instead of this process, just post a message saying there has been new trade bait and that you can message to see what it is
            
            var deserializer = new JsonResponseDeserializer();
            var info = new ResponseDeserializerInfo();
            var res = await _mfl.GetTradeBait();
            var jsonString = await res.Content.ReadAsStringAsync();
            
            try //Single
            {
                var tradeSingle = deserializer.Deserialize<TradeBaitParent>(jsonString, res, info).TradeBaits.tradeBait;
                return new List<TradeBait> { tradeSingle };
            }
            catch (Exception) { Console.WriteLine("not a single trade"); }

            try //multi
            {
                return deserializer.Deserialize<TradeBaitsParent>(jsonString, res, info).tradeBaits.tradeBait;
            }
            catch (Exception) { return null; }
            
        }

        public async Task<List<RosterPlayer>> GetRosteredPlayersByName(int year, string name)
        {
          var rosters = (await _mfl.GetRostersWithContracts(year)).rosters.franchise;
            var rosteredPlayers = rosters.SelectMany(_ => _.player).ToList();
            // build query string with all player ids in that list 
            var queryIds = "";
            rosteredPlayers.ForEach(player => queryIds = $"{queryIds}{player.id},");

            // get player details for that query
            var playerDetails = (await _mfl.GetBotPlayersDetails(queryIds)).players.player;
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
            return (await _mfl.GetAllMflPlayers()).players.player
                .Where(p => p.position == "WR" || p.position == "QB" || p.position == "TE" || p.position == "RB").ToList();
        }

        public async Task<List<LiveScoreFranchise>> GetLiveScoresForFranchises(string thisWeek)
        {
            return (await _mfl.GetLiveScores(thisWeek)).liveScoring.matchup
                .SelectMany(game => game.franchise)
                .ToList();
        }

        public async Task<List<string>> GetByesThisWeek(string thisWeek)
        {
            return (await _globalMflApi.GetByesForWeek(thisWeek)).nflByeWeeks.team?
                .Select(t => t.id).ToList() ?? new List<string>();
        }

        public async Task<List<string>> GetInjurredPlayerIdsThisWeek(string thisWeek)
        {
            return (await _globalMflApi.GetInjuries(thisWeek)).injuries.injury
                .Where(p => p.status.ToLower() != "questionable" && p.status.ToLower() != "doubtful")
                .Select(_ => _.id).ToList();
        }

        public async Task<List<FranchiseRoster>> GetFranchiseSalaries()
        {
            return (await _mfl.GetRostersWithContracts(_thisYear)).rosters.franchise;
        }

        public async Task<List<TeamAdjustedSalaryCap>> GetTeamAdjustedSalaryCaps()
        {
            return (await _mfl.GetFullLeagueDetails(_thisYear)).league.franchises.franchise
                .Select(tm => new TeamAdjustedSalaryCap()
            {
                Id = int.Parse(tm.id),
                SalaryCapAmount = string.IsNullOrEmpty(tm.salaryCapAmount) ? 500 : decimal.Parse(tm.salaryCapAmount)
            }).ToList();
        }

        public async Task<List<DraftPickTranslation>> GetFranchiseDraftPicks()
        {
            var franchises = (await _mfl.GetFranchiseAssets()).assets.franchise.Select(_ => new
            {
                DraftPicks = _.futureYearDraftPicks.draftPick.Select(x =>
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
            return franchises.SelectMany(_ => _.DraftPicks).ToList();
        }

        public async Task<List<MflFranchiseStandings>> GetFranchiseStandings()
        {
            return (await _mfl.GetStandings(_thisYear)).LeagueStandings.Franchise
                .OrderBy(tm => Int32.Parse(tm.vp))
                .ThenBy(tm => Decimal.Parse(tm.pf)).ToList();
        }
        
        
        
        
        
        
        
        
        
        
        
        public string InvertNameString(string commaName)
        {
            if (string.IsNullOrEmpty(commaName)) return "";
            var nameArr = commaName.Split(",");
            return $"{nameArr[1]} {nameArr[0]}".ToLower();
        }

    }
}
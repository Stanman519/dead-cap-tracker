using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Profiles;
using DeadCapTracker.Repositories;
using RestEase;

namespace DeadCapTracker.Services
{
    public interface ILeagueService
    {
        Task<List<TransactionDTO>> GetTransactions(int year);
        Task<List<FranchiseDTO>> UpdateFranchises(int year);
        Task<List<TeamStandings>> GetStandings(int year);
        Task<List<PendingTradeDTO>> FindPendingTrades(int year);
        Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year);
        List<DeadCapData> GetDeadCapData();
        Task<List<PlayerDetailsDTO>> GetCurrentFreeAgents(int year);
        List<TransactionDTO> GetAllTransactions();
        Task<List<StandingsV2>> GetStandingsV2(int year);
    }
    
    public class LeagueService : ILeagueService
    {
        private IMflApi _api;
        private IGlobalMflApi _globalApi;
        private readonly IMapper _mapper;
        private DeadCapTrackerContext _context;

        public LeagueService(IMflApi api, IMapper mapper, IGlobalMflApi globalApi, DeadCapTrackerContext context)
        {
            _api = api;
            _mapper = mapper;
            _globalApi = globalApi;
            _context = context;
        }

        public List<DeadCapData> GetDeadCapData()
        {
            var returnData = new List<DeadCapData>();
            //get all transactions from table and join with franchise to have team names
            var transactions =  _context.Transactions.ToList();
            var franchises = _context.Franchises.ToList();
            var allTransactions = (
                from t in transactions
                join f in franchises on t.Franchiseid equals f.Franchiseid into penalties
                from p in penalties.DefaultIfEmpty()
                    select new
                    {
                        FranchiseId = t.Franchiseid,
                        TeamName = p.Teamname,
                        DeadAmount = t.Amount,
                        PlayerName = t.Playername,
                        TransactionYear = t.Yearoftransaction,
                        NumOfYears = t.Years
                    }).ToList();
                
                // go through each transaction - add up amount for each year
                var distinct = allTransactions.GroupBy(t => t.FranchiseId)
                    .Select(grp => grp.First())
                    .Select(t => new DeadCapData(t.FranchiseId, t.TeamName))
                    .ToList();
                
                distinct.ForEach(t =>
                {
                    returnData.Add(new DeadCapData(t.FranchiseId, t.Team));
                });
                
                allTransactions.ForEach(t =>
                {
                    //get year, then get length.  add ammount to list for each year in that span. 0 = 2020
                    returnData.FirstOrDefault(_ => _.FranchiseId == t.FranchiseId)?.AddPenalties((int)t.TransactionYear, t.DeadAmount, t.NumOfYears);
                    
                });
                return returnData;
        }

        public List<TransactionDTO> GetAllTransactions()
        {
            var res = _context.Transactions.ToList();
            return _mapper.Map<List<Transaction>, List<TransactionDTO>>(res);
        }

        public async Task<List<StandingsV2>> GetStandingsV2(int year)
        {
            var modYear = year % 3;
            var yearArr = GetThreeYearArray(modYear, year);
            var dict = new Dictionary<int, MflStandingsParent>();
            var apiTasks = yearArr.Select(y => _api.GetStandings(y));
            var mflStandings = await Task.WhenAll(apiTasks);

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

        public async Task<List<TeamStandings>> GetStandings(int year)
        {
            var manualMapper = new ManualMapService();
            //figure out what year of the 3 year group we are in then make api calls based on that
            //year 1 is a 1 year 2 is a 2 year 3 is a 0
            var modYear = year % 3;
            var yearArr = GetThreeYearArray(modYear, year);
            
            var res1 = await _api.GetStandings(yearArr[0]);
            MflStandingsParent res2 = null;
            MflStandingsParent res3 = null;
            List<MflFranchiseStandings> franchiseListYr2 = null;
            List<MflFranchiseStandings> franchiseListYr3 = null;
            try
            {
                res2 = await _api.GetStandings(yearArr[1]);
            }
            catch (Exception)
            { /* ignore */ }

            try
            {
                res3 = await _api.GetStandings(yearArr[2]);
            }
            catch (Exception) { }

            var franchiseListYr1 = res1.LeagueStandings.Franchise;
            if (res2?.LeagueStandings != null) 
                franchiseListYr2 = res2.LeagueStandings.Franchise;
            if (res3?.LeagueStandings != null) 
                franchiseListYr3 = res3.LeagueStandings.Franchise;
            if (res2?.LeagueStandings == null)
                return manualMapper.MapOneYearStandings(franchiseListYr1);
            if(res3?.LeagueStandings == null)
                return manualMapper.MapTwoYearStandings(franchiseListYr1, franchiseListYr2);

            return manualMapper.MapThreeYearStandings(franchiseListYr1, franchiseListYr2, franchiseListYr3);
        }

        public async Task<List<TransactionDTO>> GetTransactions(int year)
        {
            var transactionList = (await _api.GetTransactions(year)).salaryAdjustments.salaryAdjustment;
            transactionList = SortTransactions(transactionList);
            var DTOs = _mapper.Map<List<MflTransaction>, List<TransactionDTO>>(transactionList);
            DTOs.ForEach(d => d.YearOfTransaction = d.Timestamp.Year);
            DTOs.ForEach(d => d.TransactionId = (year * 1000) + d.TransactionId);
            var latestTransId = _context.Transactions.OrderByDescending(t => t.Transactionid).Take(1).FirstOrDefault()?.Transactionid ?? 0;
            //this filter should be in a service.  keep each layer simpler

            var newEntities = _mapper.Map<List<TransactionDTO>, List<Transaction>>(DTOs).Where(t => t.Transactionid > latestTransId);
            //these should live in the repository layer
            await _context.Transactions.AddRangeAsync(newEntities);
            await _context.SaveChangesAsync();

            return DTOs;
        }
        //TODO: Needs Testing!
        public async Task<List<PendingTradeDTO>> FindPendingTrades(int year)
        {
            var DTOs = new List<PendingTradeDTO>();
            var responses = new List<Task<MflPendingTradesListRoot>>();
            for (int i = 2; i < 13; i++)
            {
                string franchiseNum = i.ToString("D4");
                responses.Add(_api.GetPendingTrades(franchiseNum));
            }
            await Task.WhenAll(responses);

            foreach (var task in responses)
            {
                var response = task.Result;
                var multiTrades = _mapper.Map<List<MflPendingTrade>, List<PendingTradeDTO>>(response.pendingTrades.pendingTrade);
                DTOs.AddRange(multiTrades);
            }
            //select only unique trade ids
            return DTOs.GroupBy(t => t.tradeId).Select(t => t.First()).ToList();
        }
        
        public async Task<List<FranchiseDTO>> UpdateFranchises(int year)
        {
            var leagueInfo = await _api.GetLeagueInfo();
            var allFranchises = leagueInfo.League.Franchises.Franchise;
            var DTOs = _mapper.Map<List<MflFranchise>, List<FranchiseDTO>>(allFranchises);

            var existingFranchiseIds = _context.Franchises
                                                .OrderBy(f => f.Franchiseid)
                                                .Select(f => f.Franchiseid)
                                                .ToList();
            var newFranchises = _mapper.Map<List<FranchiseDTO>, List<Franchise>>(DTOs).Where(_ => (!existingFranchiseIds.Contains(_.Franchiseid))).ToList();

            await _context.Franchises.AddRangeAsync(newFranchises);
            await _context.SaveChangesAsync();
            
            return DTOs;
        }

        public async Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year)
        {
            var salaries = await _api.GetSalaries();
            var oneYearPlayers = salaries.Salaries.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            
            // get names via other get call
            string queryParam = "";

            oneYearPlayers.ForEach(p => queryParam = $"{queryParam}{p.Id},");

            var playerDetails = await _globalApi.GetPlayerDetails(queryParam);

            var playerDetailsList = playerDetails.playerProfiles.playerProfile.ToList();
            //map to DTO

            var DTOs = _mapper.Map<List<MflPlayerProfile>, List<PlayerDetailsDTO>>(playerDetailsList);
            foreach (var d in DTOs)
            {
                var match = oneYearPlayers.First(_ => _.Id == d.Id);
                d.Salary = match.Salary;
            }

            return DTOs;
        }
        public async Task<List<PlayerDetailsDTO>> GetCurrentFreeAgents(int year)
        {
            
            var rawMfl = await _api.GetFreeAgents(year);
            var freeAgents = rawMfl.freeAgents.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            var freeAgents1 = new List<MflPlayer>();
            var freeAgents2 = new List<MflPlayer>();

            // get names via other get call
            string queryParam1 = "";
            string queryParam2 = "";


            freeAgents1 = freeAgents.GetRange(0, (int) Math.Floor(((decimal) freeAgents.Count) / 2));
            freeAgents2 = freeAgents.GetRange((int) Math.Floor(((decimal) freeAgents.Count) / 2), (freeAgents.Count) - (int) Math.Floor(((decimal) freeAgents.Count) / 2));

            freeAgents1.ForEach(p => queryParam1 = $"{queryParam1}{p.Id},");
            freeAgents2.ForEach(p => queryParam2 = $"{queryParam2}{p.Id},");


            var playerDetails1 = await _globalApi.GetPlayerDetails(queryParam1, year);
            var playerDetails2 = await _globalApi.GetPlayerDetails(queryParam2, year);


            var playerDetailsList = playerDetails1.playerProfiles.playerProfile.ToList();
            playerDetailsList.AddRange(playerDetails2.playerProfiles.playerProfile.ToList());
            //map to DTO

            var DTOs = _mapper.Map<List<MflPlayerProfile>, List<PlayerDetailsDTO>>(playerDetailsList);
            foreach (var d in DTOs)
            {
                var match = freeAgents.First(_ => _.Id == d.Id);
                d.Salary = match.Salary;
            }

            return DTOs;
        }

        public List<MflTransaction> SortTransactions(List<MflTransaction> transactions)
        {
            var sorted = transactions.OrderBy(t => int.Parse(t.Id)).ToList();
            return sorted;
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
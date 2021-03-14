using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using DeadCapTracker.Models;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Profiles;
using DeadCapTracker.Repositories;
using RestEase;

namespace DeadCapTracker
{
    public interface ILeagueService
    {
        Task<List<TransactionDTO>> GetTransactions(int year);
        Task<List<FranchiseDTO>> UpdateFranchises(int year);
        Task<List<TeamStandings>> GetStandings(int year);
        Task<List<PendingTradeDTO>> FindPendingTrades(int year);
        Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year);
        Task<List<DeadCapData>> GetDeadCapData();
        Task<List<PlayerDetailsDTO>> GetCurrentFreeAgents(int year);
    }
    
    public class LeagueService : ILeagueService
    {
        private IMflApi _api;
        private IGlobalMflApi _globalApi;
        private readonly IMapper _mapper;
        private DeadCapTrackerContext efdb;

        public LeagueService(IMflApi api, IMapper mapper, IGlobalMflApi globalApi, DeadCapTrackerContext context)
        {
            _api = api;
            _mapper = mapper;
            _globalApi = globalApi;
            efdb = context;
        }

        public async Task<List<DeadCapData>> GetDeadCapData()
        {
            var returnData = new List<DeadCapData>();
            //get all transactions from table and join with franchise to have team names
            var transactions =  efdb.Transactions.ToList();
            var franchises = efdb.Franchises.ToList();
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
                    .ToList();
                
                distinct.ForEach(t =>
                {
                    returnData.Add(new DeadCapData(t.FranchiseId, t.TeamName));
                });

                allTransactions.ForEach(t =>
                {
                    //get year, then get length.  add ammount to list for each year in that span. 0 = 2020
                    returnData.FirstOrDefault(_ => _.FranchiseId == t.FranchiseId).AddPenalties((int)t.TransactionYear, t.DeadAmount, t.NumOfYears);
                    
                });
                return returnData;
        }

        public async Task<List<TeamStandings>> GetStandings(int year)
        {
            var manualMapper = new ManualMapService();
            var standings = new List<TeamStandings>();
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
            catch (Exception e)
            {
                // ignored
            }

            try
            {
                res3 = await _api.GetStandings(yearArr[2]);
            }
            catch (Exception e) { }

            var franchiseListYr1 = res1.LeagueStandings.Franchise;
            if (res2?.LeagueStandings != null) {franchiseListYr2 = res2.LeagueStandings.Franchise;}
            if (res3?.LeagueStandings != null) {franchiseListYr3 = res3.LeagueStandings.Franchise;}
            if (res2?.LeagueStandings == null)
            {
                return manualMapper.MapOneYearStandings(franchiseListYr1);
            }
            else if(res3?.LeagueStandings == null)
            {
                return manualMapper.MapTwoYearStandings(franchiseListYr1, franchiseListYr2);
            }
            else 
            {
                return manualMapper.MapThreeYearStandings(franchiseListYr1, franchiseListYr2, franchiseListYr3);
            }
        }

        public async Task<List<TransactionDTO>> GetTransactions(int year)
        {
            var latestTransId = 0;
            var results = await _api.GetTransactions(year);
            var transactionList = results.salaryAdjustments.salaryAdjustment;
            transactionList = SortTransactions(transactionList);
            var DTOs = _mapper.Map<List<MflTransaction>, List<TransactionDTO>>(transactionList);
            DTOs.ForEach(d => d.YearOfTransaction = year);
            latestTransId = efdb.Transactions.OrderByDescending(t => t.Transactionid).Take(1).FirstOrDefault()?.Transactionid ?? 0;
            //this filter should be in a service.  keep each layer simpler
            var newEntities = _mapper.Map<List<TransactionDTO>, List<Transaction>>(DTOs).Where(t => t.Transactionid > latestTransId);
            //these should live in the repository layer
            await efdb.Transactions.AddRangeAsync(newEntities);
            await efdb.SaveChangesAsync();

            return DTOs;
        }
        //TODO: Needs Testing!
        public async Task<List<PendingTradeDTO>> FindPendingTrades(int year)
        {
            var DTOs = new List<PendingTradeDTO>();
            var responses = new List<Task<HttpResponseMessage>>();
            for (int i = 2; i < 13; i++)
            {
                string franchiseNum = i.ToString("D4");
                responses.Add(_api.GetPendingTrades(year, franchiseNum));
            }
            await Task.WhenAll(responses);
            var deserializer = new JsonResponseDeserializer();
            var info = new ResponseDeserializerInfo();

            foreach (var task in responses)
            {
                var response = task.Result;
                var resString = string.Empty;
                var bodyContent = response.Content.ReadAsStreamAsync().ContinueWith(t =>
                {
                    var streamRes = t.Result;
                    using (var reader = new StreamReader(streamRes))
                    {
                        resString = reader.ReadToEnd();
                    }
                }); //response.StringContent;
                bodyContent.Wait();
                if (resString.Contains("pendingTrade\":["))
                {
                    //TODO: parse response as JObject instead?? newtonsoft.json
                    var tradesList = deserializer.Deserialize<MflPendingTradesListRoot>(resString, response, info);
                    var multiTrades = _mapper.Map<List<MflPendingTrade>, List<PendingTradeDTO>>(tradesList.pendingTrades.PendingTrade);
                    DTOs.AddRange(multiTrades);
                } 
                else if (resString.Contains("pendingTrade\":{"))
                {
                    var singleTrade = deserializer.Deserialize<MflPendingSingleTradeRoot>(resString, response, info);
                    var singleDTO = _mapper.Map<MflPendingTrade, PendingTradeDTO>(singleTrade.pendingTrades.PendingTrade);
                    DTOs.Add(singleDTO);
                }
            }

            //select only unique trade ids
            return DTOs.GroupBy(t => t.tradeId).Select(t => t.First()).ToList();
        }
        
        public async Task<List<FranchiseDTO>> UpdateFranchises(int year)
        {
            var leagueInfo = await _api.GetLeagueInfo(year);
            var allFranchises = leagueInfo.League.Franchises.Franchise;
            var DTOs = _mapper.Map<List<MflFranchise>, List<FranchiseDTO>>(allFranchises);

            var existingFranchiseIds = efdb.Franchises
                                                .OrderBy(f => f.Franchiseid)
                                                .Select(f => f.Franchiseid)
                                                .ToList();
            var newFranchises = _mapper.Map<List<FranchiseDTO>, List<Franchise>>(DTOs).Where(_ => (!existingFranchiseIds.Contains(_.Franchiseid))).ToList();

            await efdb.Franchises.AddRangeAsync(newFranchises);
            await efdb.SaveChangesAsync();
            
            return DTOs;
        }

        public async Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year)
        {
            var salaries = await _api.GetSalaries(year);
            var oneYearPlayers = salaries.Salaries.LeagueUnit.Player.Where(p => p.ContractYear == "1").ToList();
            
            // get names via other get call
            string queryParam = "";

            oneYearPlayers.ForEach(p => queryParam = $"{queryParam}{p.Id},");

            var playerDetails = await _globalApi.GetPlayerDetails(year, queryParam);

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
            
            // get names via other get call
            string queryParam = "";

            freeAgents.ForEach(p => queryParam = $"{queryParam}{p.Id},");

            var playerDetails = await _globalApi.GetPlayerDetails(year, queryParam);

            var playerDetailsList = playerDetails.playerProfiles.playerProfile.ToList();
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

        private int[] GetThreeYearArray(int modYear, int year)
        {
            var yearArr = new int[3];
            if (modYear == 1)
            {
                yearArr[0] = year;
                yearArr[1] = year + 1;
                yearArr[2] = year + 2;
            }
            else if (modYear == 2)
            {
                yearArr[0] = year - 1;
                yearArr[1] = year;
                yearArr[2] = year + 1;
            }
            else
            {
                yearArr[0] = year - 2;
                yearArr[1] = year - 1;
                yearArr[2] = year;
            }
            return yearArr;
        }
        
    }
}
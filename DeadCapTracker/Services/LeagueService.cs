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
        //Task<List<TeamStandings>> GetStandings(int year);
        Task<List<PendingTradeDTO>> FindPendingTrades(int year);
        Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year);
        List<DeadCapData> GetDeadCapData();
        Task<List<PlayerDetailsDTO>> GetCurrentFreeAgents(int year);
        List<TransactionDTO> GetAllTransactions();
        Task<List<StandingsV2>> GetStandingsV2(int year);
    }
    
    public class LeagueService : ILeagueService
    {
        private readonly IMapper _mapper;
        private DeadCapTrackerContext _context;
        private readonly IGroupMePostRepo _gm;

        public IMflTranslationService _mflSvc { get; }

        //private readonly IBotPostBandaidService _bot;

        public LeagueService(IMapper mapper, DeadCapTrackerContext context, IMflTranslationService mfl, IGroupMePostRepo gm)
        {
            _mapper = mapper;
            _context = context;
            _mflSvc = mfl;
            _gm = gm;
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

            return await _mflSvc.GetStandings(year);
        }

        public async Task<List<TransactionDTO>> GetTransactions(int year)
        {
            var salaryAdjListTask = _mflSvc.GetSalaryAdjustments(year);
            var transactionsListTask = _mflSvc.GetMflTransactionsByType(year, "BBID_WAIVER");
            await Task.WhenAll(salaryAdjListTask, transactionsListTask);
            var playerLookups = transactionsListTask.Result.Select(t => t.transaction.Split(',')[0]).ToList();
            var salaryAdjList = SortTransactions(salaryAdjListTask.Result);
            var DTOs = _mapper.Map<List<MflSalaryAdjustment>, List<TransactionDTO>>(salaryAdjList);
            DTOs.ForEach(d => d.YearOfTransaction = d.Timestamp.Year);
            DTOs.ForEach(d => d.TransactionId = (year * 1000) + d.TransactionId);
            var latestTransId = _context.Transactions.OrderByDescending(t => t.Transactionid).Take(1).FirstOrDefault()?.Transactionid ?? 0;
            //this filter should be in a service.  keep each layer simpler
         
            var newEntities = _mapper.Map<List<TransactionDTO>, List<Transaction>>(DTOs).Where(t => t.Transactionid > latestTransId);
            //these should live in the repository layer
            await _context.Transactions.AddRangeAsync(newEntities);
            await _context.SaveChangesAsync();
            if (!playerLookups.Any()) return DTOs;
            var playerIds = string.Join(",", playerLookups);
            playerIds += $",{Utils.LongTermPlayerHack}";
            var playerInfos = await _mflSvc.GetMultiMflPlayers(playerIds);
            var botStr = "Waiver Wire Report:\n";
            transactionsListTask.Result.ForEach(t =>
            {
                var thisId = t.transaction.Split(',')[0];
                var salary = t.transaction.Split(',')[1].Split('|')[1];
                botStr += $"{Utils.owners[int.Parse(t.franchise)]}: {playerInfos.FirstOrDefault(p => p.id == thisId)?.name ?? ""} ${salary}\n";
            });
            await _gm.BotPost(botStr);
            return DTOs;
        }
        //TODO: Needs Testing!
        public async Task<List<PendingTradeDTO>> FindPendingTrades(int year)
        {
            return await _mflSvc.FindPendingTrades(year);
        }
        
        public async Task<List<FranchiseDTO>> UpdateFranchises(int year)
        {
            var DTOs = await _mflSvc.GetAllFranchises();
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
            var oneYearPlayers = await _mflSvc.GetPlayersOnLastYearOfContract();

            string queryParam = "";
            oneYearPlayers.ForEach(p => queryParam = $"{queryParam}{p.Id},");
            var playerDetailsList = await _mflSvc.GetMultiMflPlayerDetails(queryParam);
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
            var freeAgents = await _mflSvc.GetFreeAgents(year);
            var freeAgents1 = new List<MflPlayer>();
            var freeAgents2 = new List<MflPlayer>();

            string queryParam1 = "";
            string queryParam2 = "";

            freeAgents1 = freeAgents.GetRange(0, (int) Math.Floor(((decimal) freeAgents.Count) / 2));
            freeAgents2 = freeAgents.GetRange((int) Math.Floor(((decimal) freeAgents.Count) / 2), (freeAgents.Count) - (int) Math.Floor(((decimal) freeAgents.Count) / 2));

            freeAgents1.ForEach(p => queryParam1 = $"{queryParam1}{p.Id},");
            freeAgents2.ForEach(p => queryParam2 = $"{queryParam2}{p.Id},");

            var playerDetails1Task = _mflSvc.GetMultiMflPlayerDetails(queryParam1);
            var playerDetails2Task = _mflSvc.GetMultiMflPlayerDetails(queryParam2);
            await Task.WhenAll(playerDetails1Task, playerDetails2Task);

            var playerDetailsList = playerDetails1Task.Result;
            playerDetailsList.AddRange(playerDetails2Task.Result);
            //map to DTO

            var DTOs = _mapper.Map<List<MflPlayerProfile>, List<PlayerDetailsDTO>>(playerDetailsList);
            foreach (var d in DTOs)
            {
                var match = freeAgents.First(_ => _.Id == d.Id);
                d.Salary = match.Salary;
            }

            return DTOs;
        }

        public List<MflSalaryAdjustment> SortTransactions(List<MflSalaryAdjustment> transactions)
        {
            var sorted = transactions.OrderBy(t => int.Parse(t.Id)).ToList();
            return sorted;
        }

        
        
    }
}
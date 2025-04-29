using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DeadCapTracker.Models;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using static DeadCapTracker.Repositories.DeadCapTrackerContext;

namespace DeadCapTracker.Services
{
    public interface ILeagueService
    {
        Task<List<TransactionDTO>> GetTransactions(int year);
/*        Task<List<FranchiseDTO>> UpdateFranchises(int year);*/
        //Task<List<TeamStandings>> GetStandings(int year);
        Task<List<PendingTradeDTO>> FindPendingTrades(int leagueId, int year);
        Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int leagueId, int year);
        Task<List<DeadCapData>> GetDeadCapData();
        Task<List<PlayerDetailsDTO>> GetCurrentFreeAgents(int leagueId, int year);
        List<TransactionDTO> GetAllTransactions();
        Task<List<StandingsV2>> GetStandingsV2(int leagueId, int year);
        Task FindLatestDraftPicks(int leagueId);
        Task MapPickBudgetToOwners(int leagueId);
    }
    
    public class LeagueService : ILeagueService
    {
        private readonly IMapper _mapper;
        private DeadCapTrackerContext _context;
        private readonly IGroupMePostRepo _gm;
        private readonly ILogger<LeagueService> _logger;

        public IMflTranslationService _mflSvc { get; }

        public LeagueService(IMapper mapper, DeadCapTrackerContext context, IMflTranslationService mfl, IGroupMePostRepo gm, ILogger<LeagueService> logger)
        {
            _mapper = mapper;
            _context = context;
            _mflSvc = mfl;
            _gm = gm;
            _logger = logger;
        }

        public async Task<List<DeadCapData>> GetDeadCapData()
        {
            var returnData = new List<DeadCapData>();
            //get all transactions from table and join with franchise to have team names
            var transactions = new List<Repositories.Transaction>();
            var franchises = new List<LeagueOwnerEntity>();

            try
            {
                transactions = await _context.Transactions.ToListAsync();
                franchises = await _context.LeagueOwners.ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError("entity framework error", e);
                return new List<DeadCapData>();
            }

            var allTransactions = (
                from t in transactions
                join f in franchises on t.Franchiseid equals f.Mflfranchiseid 
                    select new
                    {
                        FranchiseId = t.Franchiseid,
                        TeamName = f.Owner.Displayname,
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
            try
            {
                var res = _context.Transactions.ToList();
                return _mapper.Map<List<Repositories.Transaction>, List<TransactionDTO>>(res);
            }
            catch (Exception e)
            {
                _logger.LogError("entity framework error", e);
                return new List<TransactionDTO>();
            }

        }

        public async Task<List<StandingsV2>> GetStandingsV2(int leagueId, int year)
        {

            return await _mflSvc.GetStandings(leagueId, year);
        }

        public async Task<List<TransactionDTO>> GetTransactions(int year)
        {
            var leagues = await _context.Leagues.Where(l => l.Mflid > 0).ToListAsync();
            var parentTaskList = new List<Task>();
            var salaryAdjTasks = new List<Task<List<MflSalaryAdjustment>>>();
            var transactionTasks = new List<Task<List<MflTransaction>>>();
            var newEntities = new List<Repositories.Transaction>();
            var playerLookups = new Dictionary<int, List<string>>();
            var botStr = "Waiver Wire Report:\n";

            foreach (var league in leagues)
            {
                salaryAdjTasks.Add(_mflSvc.GetSalaryAdjustments(league.Mflid, year));
                transactionTasks.Add(_mflSvc.GetMflTransactionsByType(league.Mflid, year, "BBID_WAIVER"));
            }
            await Task.WhenAll(parentTaskList.Concat(salaryAdjTasks).Concat(transactionTasks));

            for (var i = 0; i < leagues.Count; i++)
            {
                var salaryAdjList = SortTransactions(salaryAdjTasks[i].Result.Where(adj => !adj.Description.StartsWith("X")).ToList());
                playerLookups[leagues[i].Mflid] = transactionTasks[i].Result.Select(t => t.transaction.Split(',')[0]).ToList();
                // something is wrong with this.  if i want to post the latest adds,  I'm only looking at salary adjustments so ? ^^^
            
                var DTOs = _mapper.Map<List<MflSalaryAdjustment>, List<TransactionDTO>>(salaryAdjList);

                DTOs.ForEach(d => {
                    d.YearOfTransaction = year;
                    d.TransactionId = (year * 1000) + d.TransactionId;
                    d.LeagueId = leagues[i].Mflid;
                    });

                var entities = _mapper.Map<List<TransactionDTO>, List<Repositories.Transaction>>(DTOs);

            
                var latestTransId = _context.Transactions.Where(t => t.Leagueid == leagues[i].Mflid).OrderByDescending(t => t.Transactionid).FirstOrDefault()?.Transactionid ?? 0;
                //this filter should be in a service.  keep each layer simpler
         
                var newEntitiesForThisLeague = entities.Where(t => t.Transactionid > latestTransId && t.Leagueid == leagues[i].Mflid);
                //these should live in the repository layer
                newEntities.AddRange(newEntitiesForThisLeague);
                if (leagues[i].Botid.IsNullOrEmpty()) continue;
                if (!playerLookups[leagues[i].Mflid].Any()) continue;
                var playerIds = string.Join(",", playerLookups[leagues[i].Mflid]);
                playerIds += $",{Utils.LongTermPlayerHack}";
                var playerInfos = await _mflSvc.GetMultiMflPlayers(leagues[i].Mflid, playerIds);
                transactionTasks[i].Result.ForEach(t =>
                {
                    var thisId = t.transaction.Split(',')[0];
                    var salary = t.transaction.Split(',')[1].Split('|')[1];
                    botStr += $"{Utils.owners[int.Parse(t.franchise)]}: {playerInfos.FirstOrDefault(p => p.id == thisId)?.name ?? ""} ${salary}\n";
                });

                await _gm.BotPost(botStr, leagues[i].Botid);
            }
            if (!newEntities.Any()) return null;
            await _context.Transactions.AddRangeAsync(newEntities);
            await _context.SaveChangesAsync();

            return null;
        }
        //TODO: Needs Testing!
        public async Task<List<PendingTradeDTO>> FindPendingTrades(int leagueId, int year)
        {
            var pendingTrades =  await _mflSvc.FindPendingTrades(leagueId, year);


            return pendingTrades;
        }

        public async Task MapPickBudgetToOwners(int leagueId)
        {
            if (!Utils.leagueBotDict.TryGetValue(leagueId, out var botId)) return;
            // guard clause for off-season? too much unknown 
            var standingsTask = _mflSvc.GetFranchiseStandings(leagueId);
            var draftPicksTask = _mflSvc.GetFranchiseAssets(leagueId);
            await Task.WhenAll(standingsTask, draftPicksTask);
            var draftPicks = new List<DraftPickTranslation>();
            var standings = standingsTask.Result.ToList();
            if (draftPicksTask.Result.All(tm => tm.currentYearDraftPicks == null)) //we are in-season or offseason postdraft
            {
                var year = DateTime.UtcNow.Year + 1;
                draftPicks = _mflSvc.GetFutureFranchiseDraftPicks(draftPicksTask.Result).Where(_ => _.Year == year)
                       .OrderBy(pk => pk.Round)
                       .ThenBy(pk => pk.Pick).ToList();

                for (var rd = 1; rd < 5; rd++)
                {
                    var pickNum = 1;

                    standings.ForEach(tm =>
                    {
                        // loop through standings and assign pick values to original owner pick
                        var relevantPick = draftPicks
                            .FirstOrDefault(d => d.Year == year && d.Round == rd && d.OriginalOwner == int.Parse(tm.id));
                        relevantPick.Pick = pickNum;
                        relevantPick.SlotCost = _mflSvc.GetDraftPickPrice(rd, pickNum);
                        pickNum++;
                    });
                }
            } else
            {
                draftPicks = _mflSvc.GetCurrentFranchiseDraftPicks(draftPicksTask.Result)
                   .OrderBy(pk => pk.Round)
                   .ThenBy(pk => pk.Pick).ToList();
            }
            var ownerIds = draftPicks.Select(d => d.CurrentOwner).Distinct().ToList();
            var picksByOwner = ownerIds.Select(o => new DraftBudgetProjection(o, draftPicks.Where(d => d.CurrentOwner == o).ToList())).OrderByDescending(_ => _.RawBudget).ToList();
            var strBot = $"Projected cap for upcoming draft picks\n(max savings via taxi squad)\n-----------\n";
            picksByOwner.ForEach(o =>
            {
                strBot += $"{Utils.owners[leagueId][o.OwnerId]}: ${o.RawBudget} (${o.PotentialSavings})\n";
            });
            await _gm.BotPost(botId, strBot);
        }


        public async Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int leagueId, int year)
        {
            var oneYearPlayers = await _mflSvc.GetPlayersOnLastYearOfContract(leagueId);

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
        public async Task<List<PlayerDetailsDTO>> GetCurrentFreeAgents(int leagueId, int year)
        {
            var freeAgents = await _mflSvc.GetFreeAgents(leagueId, year);
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

        public async Task FindLatestDraftPicks(int leagueId)
        {
            var year = DateTime.Now.Year;
            DateTime oneHourAgo = DateTime.Now.AddHours(-1);
            var picksWithValuesTask = _mflSvc.GetDraftPicksAndContractValues(leagueId);
            var salariesTask = _mflSvc.GetAllSalaries(leagueId, year);
            await Task.WhenAll(picksWithValuesTask, salariesTask);
            var playersWithoutSalaries = salariesTask.Result.Where(p => (string.IsNullOrEmpty(p.Salary) || p.Salary == "0")).ToList();
            playersWithoutSalaries.ForEach(async p =>
            {
                //find draft pick
                var foundDraftPick = picksWithValuesTask.Result.FirstOrDefault(_ => _.Player == p.Id);
                //if exists, post contract
                if (foundDraftPick != null)
                {
                    var success = int.TryParse(foundDraftPick.Player, out var safeId);
                    if (success)
                    {
                        await _mflSvc.GiveNewContractToPlayer(leagueId, safeId, foundDraftPick.Salary, foundDraftPick.Length);
                    }

                }
            });           
        }

    }
}
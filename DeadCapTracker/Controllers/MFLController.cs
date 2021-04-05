using System.Collections.Generic;
using System.Threading.Tasks;
using DeadCapTracker.Models;
using DeadCapTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DeadCapTracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MflController : ControllerBase
    {
        private ILeagueService _leagueService;

        public MflController(ILeagueService leagueService, DeadCapTrackerContext context)
        {
            _leagueService = leagueService;
        }
        [HttpGet("transactions/{year}")]
        public async Task<List<TransactionDTO>> GetTransactions(int year)
        {
            return await _leagueService.GetTransactions(year);
        }
        [HttpGet("allTransactions/")]
        public async Task<List<TransactionDTO>> GetAllTransactions()
        {
            return _leagueService.GetAllTransactions();
        }
        
        [HttpGet("franchises/{year}")]
        public async Task<List<FranchiseDTO>> GetFranchises(int year)
        {
            var franchiseList = await _leagueService.UpdateFranchises(year);
            return franchiseList;
        }
        
        [HttpGet("standings/{year}")]
        public async Task<List<TeamStandings>> GetStandings(int year)
        {
            var franchiseList = await _leagueService.GetStandings(year);
            return franchiseList;
        }

        [HttpGet("pendingTrades/{year}")]
        public async Task<List<PendingTradeDTO>> GetPendingTrades(int year)
        {
            return await _leagueService.FindPendingTrades(year);
        }
        
        [HttpGet("impendingFreeAgents/{year}")]
        public async Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year)
        {
            return await _leagueService.GetImpendingFreeAgents(year);
        }
        
        [HttpGet("currentFreeAgents/{year}")]
        public async Task<List<PlayerDetailsDTO>> GetAllFreeAgents(int year)
        {
            return await _leagueService.GetCurrentFreeAgents(year);
        }
        
        [HttpGet("deadCapInfo")]
        public async Task<List<DeadCapData>> GetDeadCapPenalties()
        {
            return await _leagueService.GetDeadCapData();
        }

    }
    
    //TODO: api versioning
}
    
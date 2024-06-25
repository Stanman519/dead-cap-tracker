using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeadCapTracker.Models;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Repositories;
using DeadCapTracker.Services;
using Microsoft.AspNetCore.Mvc;
using RestEase;

namespace DeadCapTracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MflController : ControllerBase
    {
        private ILeagueService _leagueService;

        public MflController(ILeagueService leagueService)
        {
            _leagueService = leagueService;
        }
        [HttpGet("transactions")]
        public async Task<List<TransactionDTO>> GetTransactions()
        {
            var year = DateTime.Now.Year; 
            return await _leagueService.GetTransactions(year);
        }
        [HttpGet("allTransactions")]
        public List<TransactionDTO> GetAllTransactions()
        {
            return _leagueService.GetAllTransactions();
        }
        
/*        [HttpGet("franchises/{year}")]
        public async Task<List<FranchiseDTO>> GetFranchises(int year)
        {
            return await _leagueService.UpdateFranchises(year);
        }*/
        
        [HttpGet("leagues/{leagueId}/years/{year}/standings/")]
        public async Task<List<StandingsV2>> GetStandings(int leagueId, int year)
        {
            return await _leagueService.GetStandingsV2(leagueId, year);
        }

        [HttpGet("leagues/{leagueId}/pending-trades")]
        public async Task<List<PendingTradeDTO>> GetPendingTrades([Path] int leagueId)
        {
            var year = DateTime.UtcNow.Year;
            return await _leagueService.FindPendingTrades(leagueId, year);
        }
        
/*        [HttpGet("impendingFreeAgents/{year}")]
        public async Task<List<PlayerDetailsDTO>> GetImpendingFreeAgents(int year)
        {
            return await _leagueService.GetImpendingFreeAgents(year);
        }*/
        
        [HttpGet("leagues/{leagueId}/years/{year}/current-free-agents")]
        public async Task<List<PlayerDetailsDTO>> GetAllFreeAgents(int leagueId, int year)
        {
            return await _leagueService.GetCurrentFreeAgents(leagueId, year);
        }
        
        [HttpGet("deadCapInfo")]
        public async Task<List<DeadCapData>> GetDeadCapPenalties()
        {
            return await _leagueService.GetDeadCapData();
        }
        [HttpGet("draft-sync/{leagueId}")]
        public async Task SyncRookieDraftContracts([Path] int leagueId)
        {
            await _leagueService.FindLatestDraftPicks(leagueId);
        }
    }
    
    //TODO: api versioning
}
    
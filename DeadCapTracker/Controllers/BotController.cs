using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Services;
using Microsoft.AspNetCore.Mvc;
using RestEase;

namespace DeadCapTracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BotController : ControllerBase
    {
        private IGroupMeRequestService _groupMeRequestService;

        public BotController(IGroupMeRequestService groupMeRequestService)
        {
            _groupMeRequestService = groupMeRequestService;
        }

        [HttpGet("standings/{year}")]
        public async Task<List<AnnualScoringData>> PostStandings(int year)
        {
            try
            {
                return await _groupMeRequestService.PostStandingsToGroup(year);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        [HttpGet("pendingTrades/{year}")]
        public async Task<List<PendingTradeDTO>> PostTradeOffers(int year)
        {
            return await _groupMeRequestService.PostTradeOffersToGroup(year);
        }

        [HttpGet("tradeBait")]
        public async Task PostTradeRumor()
        {
            await _groupMeRequestService.PostTradeRumor();
        }

        [HttpGet("completedTrades/{year}")]
        public async Task PostCompletedTrades(int year)
        {
            await _groupMeRequestService.PostCompletedTradeToGroup();
        }

        [HttpPost("auctionError")]
        public async Task PostAuctionError([FromBody] ErrorMessage error)
        {
            await _groupMeRequestService.BotPost(error.Message);
        }

        [HttpPost("contractSearch/{year}")]
        public async Task<string> ContractSearch([FromBody] GmMessage message, int year)
        {
            var request = message.text.ToLower();
            var isContractRequest = request.StartsWith("#contract");
            var isScoresRequest = request.StartsWith("#scores");
            var isLineupChecker = request.StartsWith("#lineups");
            var isStandings = request.StartsWith("#standings");
            var isCapSpace = request.StartsWith("#cap");
            var isDraftPickReq = request.StartsWith("#draft");
            var isFreeAgentRequest = request.StartsWith("#free");
            var isFranchiseTag = request.StartsWith("#tag");
            var isHelp = request.StartsWith("#help");
            var isDeadCap = request.StartsWith("#dead");
            var strayTag = request.Contains("@cap") || request.Contains("@the cap") || request.Contains("@thec");
            
            if (!isContractRequest && !isScoresRequest && !isLineupChecker && !isStandings && !isHelp && !strayTag && !isCapSpace && !isDraftPickReq && !isFreeAgentRequest && !isFranchiseTag && !isDeadCap)
                return null;
            
            if (isContractRequest)
            {
                var capIndex = message.text.ToLower().IndexOf("#contract", StringComparison.Ordinal);
                var searchText = message.text.Remove(capIndex, 10);
                return await _groupMeRequestService.FindAndPostContract(Utils.ThisYear, searchText.ToLower());
            }

            if (isFranchiseTag) await _groupMeRequestService.PostFranchiseTagAmounts();

            if (isScoresRequest)
                return await _groupMeRequestService.FindAndPostLiveScores();

            if (isLineupChecker) await _groupMeRequestService.CheckLineupsForHoles();

            if (isHelp) await _groupMeRequestService.PostHelpMessage();

            if (isStandings) await _groupMeRequestService.PostStandingsToGroup(Utils.ThisYear);

            if (isCapSpace) await _groupMeRequestService.PostCapSpace();

            if (isFreeAgentRequest) await _groupMeRequestService.PostTopUpcomingFreeAgents(request.Split(" ")[1]);
            // add available free agents
            
            if (strayTag) await _groupMeRequestService.StrayTag();

            if (isDraftPickReq) await _groupMeRequestService.PostDraftProjections(Utils.ThisYear);

            if (isDeadCap) await _groupMeRequestService.PostFutureDeadCap();

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
        private IGmFreeAgencyService _gmFA;
        private IGroupMeRequestService _groupMeRequestService;

        public BotController(IGroupMeRequestService groupMeRequestService, IGmFreeAgencyService gmFA)
        {
            _gmFA = gmFA;
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
            await _groupMeRequestService.BotPost(error.Message, true);
        }

        [HttpPost("stanfan-msg")]
        public async Task PostMessageFromStanfan([FromBody] ErrorMessage error)
        {
            await _groupMeRequestService.BotPost(error.Message, false);
        }


        [HttpPost("contractSearch/{year}")]
        public async Task<string> ContractSearch([FromBody] GmMessage message, int year)
        {
            var request = message.text.ToLower();
            var isContractRequest = request.Contains("#contract");
            var isScoresRequest = request.Contains("#scores");
            var isLineupChecker = request.Contains("#lineups");
            var isStandings = request.Contains("#standings");
            var isCapSpace = request.Contains("#cap");
            var isDraftPickReq = request.Contains("#draft");
            var isFreeAgentRequest = request.Contains("#free");
            var isFranchiseTag = request.Contains("#tag");
            var isHelp = request.Contains("#help");
            var isDeadCap = request.Contains("#dead");
            var isDraftCost = request.Contains("#budget");
            var isQuickBid = request.Contains("#bid");
            var strayTag = request.Contains("@cap") || request.Contains("@the cap") || request.Contains("@thec");
            
            if (!isContractRequest && !isScoresRequest && !isLineupChecker && !isStandings && !isHelp && !strayTag && !isCapSpace && !isDraftPickReq && !isFreeAgentRequest && !isFranchiseTag && !isDeadCap && !isDraftCost && !isQuickBid)
                return null;

            var groupId = message.group_id;

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

            if (isCapSpace) await _groupMeRequestService.PostCapSpace(Utils.GmGroupToMflLeague.FirstOrDefault(t => t.Item1 == groupId).Item2);

            if (isFreeAgentRequest)
            {
                var reqArr = request.Split(" ");
                if (reqArr.Length < 2) return null;
                int faYear = 0;
                var isValidYear = reqArr.Length > 2 && int.TryParse(reqArr[2], out faYear);
                isValidYear = faYear > 2020 && faYear < Utils.ThisYear + 3;
                await _groupMeRequestService.PostTopUpcomingFreeAgents(reqArr[1], isValidYear ? faYear : Utils.ThisYear + 1);
            }
            // add available free agents
            
            if (strayTag) await _groupMeRequestService.StrayTag();

            if (isDraftPickReq) await _groupMeRequestService.PostDraftProjections(Utils.ThisYear);

            if (isDeadCap) await _groupMeRequestService.PostFutureDeadCap();

            if (isDraftCost) await _groupMeRequestService.PostDraftBudgets();

            if (isQuickBid) await _gmFA.PostQuickBidByLotId(message);

            return null;
        }
    }
}

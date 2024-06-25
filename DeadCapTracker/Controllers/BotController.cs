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
        public async Task PostStandings(int year)
        {
            try
            {
                var leagues = Utils.GmGroupToMflLeague;
                foreach (var item in leagues)
                {
                    await _groupMeRequestService.PostStandingsToGroup(item.Item2, year);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }

        [HttpGet("pendingTrades")]
        public async Task PostTradeOffers()
        {
            var year = DateTime.Now.Year;
            var leagues = Utils.GmGroupToMflLeague;
            foreach (var item in leagues)
            {
                await _groupMeRequestService.PostTradeOffersToGroup(item.Item2, year);
            }

        }

        [HttpGet("tradeBait")]
        public async Task PostTradeRumor()
        {
            var leagues = Utils.GmGroupToMflLeague;
            foreach (var item in leagues)
            {
                await _groupMeRequestService.PostTradeRumor(item.Item2);
            }
        }

        [HttpGet("completedTrades")]
        public async Task PostCompletedTrades()
        {
            var leagues = Utils.GmGroupToMflLeague;
            foreach (var item in leagues)
            {
                await _groupMeRequestService.PostCompletedTradeToGroup(item.Item2);
            }

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


        [HttpPost("contractSearch/{fakeYear}")]
        public async Task<string> ContractSearch([FromBody] GmMessage message, int fakeYear)
        {
            var year = DateTime.Now.Year;
            var request = message.text.ToLower();
            var groupId = message.group_id;
            var leagueId = Utils.GmGroupToMflLeague.FirstOrDefault(t => t.Item1 == groupId).Item2;
            var actions = new Dictionary<string, Func<Task<string>>>
            {
                ["#contract"] = async () =>
                {
                    var capIndex = message.text.IndexOf("#contract", StringComparison.Ordinal);
                    var searchText = message.text.Remove(capIndex, 10);
                    return await _groupMeRequestService.FindAndPostContract(leagueId, year, searchText.ToLower());
                },
                ["#scores"] = () => _groupMeRequestService.FindAndPostLiveScores(leagueId),
                ["#lineups"] = async () => { await _groupMeRequestService.CheckLineupsForHoles(leagueId); return null; },
                ["#standings"] = async () => { await _groupMeRequestService.PostStandingsToGroup(leagueId, year); return null; },
                ["#cap"] = async () => { await _groupMeRequestService.PostCapSpace(leagueId); return null; },
                ["#draft"] = async () => { await _groupMeRequestService.PostDraftProjections(leagueId, year); return null; },
                ["#free"] = async () =>
                {
                    var reqArr = request.Split(" ");
                    if (reqArr.Length < 2) return null;
                    int faYear = 0;
                    var isValidYear = reqArr.Length > 2 && int.TryParse(reqArr[2], out faYear);
                    isValidYear = faYear > 2020 && faYear < year + 3;
                    await _groupMeRequestService.PostTopUpcomingFreeAgents(leagueId, reqArr[1], isValidYear ? faYear : year + 1);
                    return "";
                },
                ["#tag"] = async () => { await _groupMeRequestService.PostFranchiseTagAmounts(leagueId); return null; },
                ["#help"] = async () => { await _groupMeRequestService.PostHelpMessage(); return null; },
                ["#dead"] = async () => { await _groupMeRequestService.PostFutureDeadCap(); return null; },
                ["#budget"] = async () => { await _groupMeRequestService.PostDraftBudgets(leagueId); return null; },
                ["#bid"] = async () => { await _gmFA.PostQuickBidByLotId(message); return null; },
                ["@cap"] = async () => { await _groupMeRequestService.StrayTag(); return null; },
                ["@the cap"] = async () => { await _groupMeRequestService.StrayTag(); return null; },
                ["@thec"] = async () => { await _groupMeRequestService.StrayTag(); return null; }
            };

            foreach (var action in actions)
            {
                if (request.Contains(action.Key))
                {
                    return await action.Value();
                }
            }

            return null;
        }
    }
}

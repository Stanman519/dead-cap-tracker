using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
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
                    if (!Utils.leagueBotDict.TryGetValue(item.Item2, out var botId)) continue;
                    await _groupMeRequestService.PostStandingsToGroup(botId, item.Item2, year);
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
                if (!Utils.leagueBotDict.TryGetValue(item.Item2, out var botId)) continue;
                await _groupMeRequestService.PostTradeOffersToGroup(botId, item.Item2, year, item.Item1);
            }

        }

        [HttpGet("tradeBait")]
        public async Task PostTradeRumor()
        {
            var leagues = Utils.GmGroupToMflLeague;
            foreach (var item in leagues)
            {
                if (!Utils.leagueBotDict.TryGetValue(item.Item2, out var botId)) continue;
                await _groupMeRequestService.PostTradeRumor(botId, item.Item2);
            }
        }

        [HttpGet("completedTrades")]
        public async Task PostCompletedTrades()
        {
            var leagues = Utils.GmGroupToMflLeague;
            foreach (var item in leagues)
            {
                if (!Utils.leagueBotDict.TryGetValue(item.Item2, out var botId)) continue;
                await _groupMeRequestService.PostCompletedTradeToGroup(botId, item.Item2);
            }

        }

        [HttpPost("auctionError")]
        public async Task PostAuctionError([FromBody] GmBotMessage msg)
        {

            await _groupMeRequestService.BotPost(string.Empty, msg.Message, true);
        }

        [HttpPost("stanfan-msg")]
        public async Task PostMessageFromStanfan([FromBody] GmBotMessage msg)
        {
            await _groupMeRequestService.BotPost(msg.BotId, msg.Message, false);
        }


        [HttpPost("contractSearch/{fakeYear}")]
        public async Task<string> ContractSearch([FromBody] GmMessage message, int fakeYear)
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var request = message.text.ToLower();
            var groupId = message.group_id;
            var leagueId = Utils.GmGroupToMflLeague.FirstOrDefault(t => t.Item1 == groupId).Item2;
            var gmId = message.sender_id;

            if (!Utils.leagueBotDict.TryGetValue(leagueId, out var botId)) return "";
            var actions = new Dictionary<string, Func<Task<string>>>
            {
                ["#contract"] = async () =>
                {
                    var capIndex = message.text.IndexOf("#contract", StringComparison.Ordinal);
                    var searchText = message.text.Remove(capIndex, 10);
                    return await _groupMeRequestService.FindAndPostContract(botId, leagueId, year, searchText.ToLower());
                },
                ["#scores"] = () => _groupMeRequestService.FindAndPostLiveScores(botId, leagueId),
                ["#lineups"] = async () => { await _groupMeRequestService.CheckLineupsForHoles(botId, leagueId, message.group_id); return null; },
                ["#standings"] = async () => { await _groupMeRequestService.PostStandingsToGroup(botId, leagueId, year); return null; },
                ["#cap"] = async () => { await _groupMeRequestService.PostCapSpace(botId, leagueId); return null; },
                ["#draft"] = async () => { await _groupMeRequestService.PostDraftProjections(botId, leagueId, year); return null; },
                ["#free"] = async () =>
                {
                    var reqArr = request.Split(" ");
                    if (reqArr.Length < 2) return null;
                    int faYear = 0;
                    var isValidYear = reqArr.Length > 2 && int.TryParse(reqArr[2], out faYear);
                    isValidYear = faYear > 2020 && faYear < year + 3;
                    var isOffseason = month < 9;
                    await _groupMeRequestService.PostTopUpcomingFreeAgents(botId, leagueId, reqArr[1], isValidYear ? faYear : isOffseason ? year : year + 1);
                    return "";
                },
                ["#optimize"] = async () => { await _groupMeRequestService.OptimizeLineup(botId, leagueId, gmId); return null; },
                ["#tag"] = async () => { await _groupMeRequestService.PostFranchiseTagAmounts(botId, leagueId); return null; },
                ["#help"] = async () => { await _groupMeRequestService.PostHelpMessage(botId); return null; },
                ["#dead"] = async () => { await _groupMeRequestService.PostFutureDeadCap(botId, leagueId); return null; },
                ["#budget"] = async () => { await _groupMeRequestService.PostDraftBudgets(botId, leagueId); return null; },
                ["#bid"] = async () => { await _gmFA.PostQuickBidByLotId(message); return null; },
                ["@cap"] = async () => { await _groupMeRequestService.StrayTag(botId); return null; },
                ["@the cap"] = async () => { await _groupMeRequestService.StrayTag(botId); return null; },
                ["@thec"] = async () => { await _groupMeRequestService.StrayTag(botId); return null; }
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

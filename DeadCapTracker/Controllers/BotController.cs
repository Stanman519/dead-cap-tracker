﻿using System;
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
        private IGroupMeService _groupMeService;

        public BotController(IGroupMeService groupMeService)
        {
            _groupMeService = groupMeService;
        }

        [HttpGet("standings/{year}")]
        public async Task<List<TeamStandings>> PostStandings(int year)
        {
            try
            {
                return await _groupMeService.PostStandingsToGroup(year);
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
            return await _groupMeService.PostTradeOffersToGroup(year);
        }

        [HttpGet("tradeBait")]
        public async Task PostTradeRumor()
        {
            await _groupMeService.PostTradeRumor();
        }

        [HttpGet("completedTrades/{year}")]
        public async Task PostCompletedTrades(int year)
        {
            await _groupMeService.PostCompletedTradeToGroup();
        }

        [HttpPost("auctionError")]
        public async Task PostCompletedTrades([FromBody] ErrorMessage error)
        {
            await _groupMeService.BotPost(error.Message);
        }

        [HttpPost("contractSearch/{year}")]
        public async Task<string> ContractSearch([FromBody] GmMessage message, int year)
        {
            var request = message.text.ToLower();
            var isContractRequest = request.StartsWith("#contract");
            var isScoresRequest = request.StartsWith("#scores");
            var isLineupChecker = request.StartsWith("#lineups");
            var isStandings = request.StartsWith("#standings");
            var isHelp = request.StartsWith("#help");
            
            if (!isContractRequest && !isScoresRequest && !isLineupChecker && isStandings && !isHelp)
                return null;
            
            
            if (isContractRequest)
            {
                var capIndex = message.text.ToLower().IndexOf("#contract", StringComparison.Ordinal);
                var searchText = message.text.Remove(capIndex, 10);
                return await _groupMeService.FindAndPostContract(year, searchText.ToLower());
            }

            if (isScoresRequest)
                return await _groupMeService.FindAndPostLiveScores();

            if (isLineupChecker) await _groupMeService.CheckLineupsForHoles();

            if (isHelp) await _groupMeService.PostHelpMessage();

            if (isStandings) await _groupMeService.PostStandingsToGroup(year);
            
            return null;
        }
    }
}
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeadCapTracker.Services
{
    public interface IGmFreeAgencyService
    {
        Task PostQuickBidByLotId(GmMessage message);
    }
    public class GmFreeAgencyService : IGmFreeAgencyService
    {
        private readonly IFreeAgencyAuctionAPI _auctionAPI;
        private readonly IGroupMePostRepo _gm;
        private readonly DeadCapTrackerContext _context;
        private readonly Dictionary<int, Dictionary<int, string>> _members;

        public GmFreeAgencyService(DeadCapTrackerContext context, IFreeAgencyAuctionAPI auctionAPI, IGroupMePostRepo gm)
        {
            _auctionAPI = auctionAPI;
            _gm = gm;
            _context = context;
            _members = Utils.memberIds;
        }

        public async Task PostQuickBidByLotId(GmMessage message)
        {
            // sanitize message - get lotId and franchise id from user
            try
            {
                var leagueId = Utils.GmGroupToMflLeague.FirstOrDefault(t => t.Item1 == message.group_id)?.Item2 ?? 0;
                var lotId = GetSanitizedLotId(message.text);
                var franchiseId = _members[leagueId].FirstOrDefault(m => m.Value == message.sender_id, new KeyValuePair<int, string>(-1, "")).Key;
                if (franchiseId == -1) throw new ArgumentException("Unable to find user's franchise.");
                var lot = await _context.Lots.FindAsync(lotId);
                if (lot == null) throw new ArgumentException("Unable to find lot.");
                if (lot.Bidid == null) throw new ArgumentException("This lot is empty and has no current bid.");
                if (lot.Leagueid != leagueId) throw new ArgumentException("Lot submitted is not assigned to this league.");
                if (lot.Bid.LeagueOwner.Mflfranchiseid == franchiseId) throw new ArgumentException("You are already the high bidder.");
                var leagueOwner = await _context.LeagueOwners.FirstOrDefaultAsync(l => l.Mflfranchiseid == franchiseId && l.Leagueid == leagueId);
                if (leagueOwner == null) throw new ArgumentException("Could not find franchise owner.");
                var bidDTO = new BidDTO
                {
                    LotId = lotId,
                    BidLength = lot.Bid.Bidlength,
                    BidSalary = lot.Bid.Bidsalary + 1,
                    LeagueId = lot.Bid.Leagueid,
                    OwnerId = leagueOwner.Leagueownerid,
                    Player = new PlayerDTO
                    {
                        MflId = lot.Bid.Mflid
                    }
                };
                var res = await _auctionAPI.PostNewBid(bidDTO);
                await _gm.BotPost($"New Bid (lot {res.LotId}):\n{res.Ownername}\n{res.Player.Position} {res.Player.LastName}\n{res.BidLength} yr/${res.BidSalary}");
            }
            catch (Exception e)
            {
                await _gm.BotPost(e.Message);
            }


            // get current lot from db - return if invalid (ie. not found/bid is null/current leading bid is same user)

            // honky dory ok insert bid via FA post bid endpoint.
            // if it returns successfully, post new bid confirmation to group


            
        }

        public static int GetSanitizedLotId(string input)
        {
            // Regular expression pattern to match "#bid" followed by digits
            string pattern = @"#bid\s*(\d+)";

            // Match the pattern in the input string
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                // Extract and parse the digits
                string numberString = match.Groups[1].Value;
                if (int.TryParse(numberString, out int number))
                {
                    return number;
                }
            }

            // Return -1 or throw an exception if the pattern is not found or the number cannot be parsed
            throw new ArgumentException("Unable to parse lot Id");
        }
    }
}

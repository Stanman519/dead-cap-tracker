using DeadCapTracker.Models.DTOs;
using RestEase;
using System.Threading.Tasks;

namespace DeadCapTracker.Repositories
{
    public interface IFreeAgencyAuctionAPI
    {
        [Post("free-agency/bid")]
        Task<BidDTO> PostNewBid([Body] BidDTO bid);
    }
}

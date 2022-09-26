using System.Threading.Tasks;

namespace DeadCapTracker.Services
{
    public interface IBotPostBandaidService
    {
        Task BotPostPassThrough(string post);
    }

    public class BotPostBandaidService : IBotPostBandaidService
    {
        private readonly IGroupMeRequestService _bot;

        public BotPostBandaidService(IGroupMeRequestService bot)
        {
            _bot = bot;
        }
        public async Task BotPostPassThrough(string post)
        {
            await _bot.BotPost(post);
        }
    }
}

using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using RestEase;

namespace DeadCapTracker.Repositories
{
    public interface IGroupMeApi
    {
        [Post("v3/bots/post")]
        Task SendMessage([Body]Message message);
        
        [Get("v3/groups/59795205?token=YjMEBw8kwXkJMKxDz2nd2o0iG9aC1GG4NjD9O1ih")]
        Task<GroupParent> GetMemberIds();

    }

}
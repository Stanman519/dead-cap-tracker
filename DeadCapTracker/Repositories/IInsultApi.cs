using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using RestEase;

namespace DeadCapTracker.Repositories
{
    public interface IInsultApi
    {
        [Get("generate_insult.php?lang=en&type=json")]
        Task<Insult> GetInsult();
    }
}
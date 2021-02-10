using System.Net.Http;
using System.Threading.Tasks;
using DeadCapTracker.Models.MFL;
using Microsoft.AspNetCore.Http;
using RestEase;

namespace DeadCapTracker.Repositories
{
    public interface IGlobalMflApi
    {
        [Get("{year}/export?TYPE=playerProfile&P={ids}&JSON=1")]
        Task<MflPlayerProfilesRoot> GetPlayerDetails([Path] int year, [Path] string ids);
    }
}

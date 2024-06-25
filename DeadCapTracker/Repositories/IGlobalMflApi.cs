using System.Net.Http;
using System.Threading.Tasks;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Services;
using Microsoft.AspNetCore.Http;
using RestEase;

namespace DeadCapTracker.Repositories
{
    public interface IGlobalMflApi
    {
        [Get("{year}/export?TYPE=playerProfile&P={ids}&JSON=1")]
        Task<MflPlayerProfilesRoot> GetPlayerDetails([Path] string ids, [Path] int year);

        [Get("{year}/export?TYPE=nflByeWeeks&W={week}&JSON=1")]
        Task<MflByesRoot> GetByesForWeek([Path] string week, [Path] int year);

        [Get("{year}/export?TYPE=injuries&W={thisWeek}&JSON=1")]
        Task<InjuriesRoot> GetInjuries([Path] string thisWeek, [Path] int year);
    }
}

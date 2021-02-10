using System.Net.Http;
using System.Threading.Tasks;
using DeadCapTracker.Models.MFL;
using Microsoft.AspNetCore.Http;
using RestEase;

namespace DeadCapTracker.Repositories
{
    public interface IMflApi
    {
        [Get("{year}/export?TYPE=salaryAdjustments&L=13894&APIKEY=&JSON=1")]
        Task<ParentTransaction> GetTransactions([Path] int year);
        
        [Get("{year}/export?TYPE=league&L=13894&APIKEY=&JSON=1")]
        Task<LeagueInfo> GetLeagueInfo([Path] int year);
        
        // TODO: eventually add: trade announcements

        [Get("{year}/export?TYPE=leagueStandings&L=13894&APIKEY=ahBv1sGSvuWvx1GmOVrJaDYeFLox&JSON=1")]
        Task<MflStandingsParent> GetStandings([Path] int year);
        
        [Get("{year}/export?TYPE=pendingTrades&L=13894&APIKEY=ahBv1sGSvuWvx1GmOVrJaDYeFLox&FRANCHISE_ID=&JSON=1")]
        Task<HttpResponseMessage> GetPendingTrades([Path] int year);

        [Get("{year}/export?TYPE=freeAgents&L=13894&APIKEY=ahBv1sGSvuWvx1GmOVrJaDYeFLox&JSON=1")]
        Task<MflAgentsParent> GetFreeAgents([Path] int year);
        
        [Get("{year}/export?TYPE=salaries&L=13894&APIKEY=ahBv1sGSvuWvx1GmOVrJaDYeFLox&JSON=1")]
        Task<MflSalariesParent> GetSalaries([Path] int year);
        
        [Get("{year}/export?TYPE=playerProfile&P={ids}&JSON=1")]
        Task<MflPlayerProfilesRoot> GetPlayerDetails([Path] int year, [Path] string ids);
    }
}
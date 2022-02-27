using System.Net.Http;
using System.Threading.Tasks;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Services;
using RestEase;

namespace DeadCapTracker.Repositories
{
    public interface IMflApi
    {
        // TODO: add API key to UTILs
        [Get("{year}/export?TYPE=salaryAdjustments&L=13894&APIKEY=&JSON=1")]
        Task<ParentTransaction> GetTransactions([Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=league&L=13894&APIKEY=&JSON=1")]
        Task<LeagueInfo> GetLeagueInfo([Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=league&L=13894&APIKEY=&JSON=1")]
        Task<LeagueRoot> GetFullLeagueDetails([Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=leagueStandings&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&JSON=1")]
        Task<MflStandingsParent> GetStandings([Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=pendingTrades&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&FRANCHISE_ID={franchiseNum}&JSON=1")]
        Task<HttpResponseMessage> GetPendingTrades( [Path] string franchiseNum, [Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=freeAgents&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&JSON=1")]
        Task<MflAgentsParent> GetFreeAgents([Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=salaries&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&JSON=1")]
        Task<MflSalariesParent> GetSalaries([Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=playerProfile&P={ids}&JSON=1")]
        Task<MflPlayerProfilesRoot> GetPlayerDetails([Path] string ids, [Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=transactions&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&W=&TRANS_TYPE=TRADE&FRANCHISE=&DAYS=1&COUNT=&JSON=1")]
        Task<HttpResponseMessage> GetRecentTrade([Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=tradeBait&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&INCLUDE_DRAFT_PICKS=true&JSON=1")]
        Task<HttpResponseMessage> GetTradeBait([Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=players&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&DETAILS=&SINCE=&PLAYERS={player}&JSON=1")]
        Task<PlayerParent> GetBotPlayerDetails([Path] string player, [Path] int year = Utils.ThisYear);
        
        [Get("{year}/export?TYPE=players&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&DETAILS=&SINCE=&PLAYERS={players}&JSON=1")]
        Task<PlayersParent> GetBotPlayersDetails([Path] string players, [Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=rosters&L=13894&APIKEY=&FRANCHISE=&W=&JSON=1")]
        Task<MflRostersRoot> GetRostersWithContracts([Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=liveScoring&L=13894&APIKEY=&W={weekNum}&DETAILS=&JSON=1")]
        Task<LiveScoreRoot> GetLiveScores([Path] string weekNum, [Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=projectedScores&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&W={weekNum}&PLAYERS=&POSITION=&STATUS=&COUNT=&JSON=1")]
        Task<ProjectedScoresRoot> GetProjections([Path] string weekNum, [Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=schedule&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&W=&F=&JSON=1")]
        Task<ScheduleRoot> GetMatchupSchedule([Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=players&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&DETAILS=&SINCE=&PLAYERS=&JSON=1")]
        Task<PlayersParent> GetAllMflPlayers([Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=assets&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&JSON=1")]
        Task<MflAssetsRoot> GetFranchiseAssets([Path] int year = Utils.ThisYear);

        [Get("{year}/export?TYPE=playerScores&L=13894&APIKEY=ahBv1sGSvuWux1OmOVrJaDYeFLox&W=AVG&YEAR=&PLAYERS=&POSITION=&STATUS=&RULES=&COUNT=&JSON=1")]
        Task<MflAverageScoreRoot> GetAveragePlayerScores([Path] int year = Utils.ThisYear);
    }
    
}
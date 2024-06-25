using System.Collections.Generic;
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
        [Header("cookie", "MFL_IS_COMMISH=K1E6gJXC6uSu0xW6O1HIZQ%3D%3D;MFL_USER_ID=ahBv1sGSvqLri1vmaQfEYDs%3D")]
        [Post("{year}/import?TYPE=salaries&L={leagueId}&APPEND=1")]
        Task<HttpResponseMessage> EditPlayerSalary([Path] int leagueId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> data, [Path] int year);


        // TODO: add API key to UTILs
        [Get("{year}/export?TYPE=salaryAdjustments&L={leagueId}&APIKEY=&JSON=1")]
        Task<MflSalaryAdjParent> GetSalaryAdjustments([Path] int leagueId, [Path] int year);
        [Get("{year}/export?TYPE=transactions&L={leagueId}&APIKEY=&W=&TRANS_TYPE=&FRANCHISE=&DAYS={previousDays}&COUNT=&JSON=1")]
        Task<MflTransactionsParent> GetMflTransactions([Path] int leagueId, [Path] int year, [Path] int previousDays = 1);
        [Get("{year}/export?TYPE=league&L={leagueId}&APIKEY=&JSON=1")]
        Task<LeagueInfo> GetLeagueInfo([Path] int leagueId, [Path] int year);
        
        [Get("{year}/export?TYPE=league&L={leagueId}&APIKEY=&JSON=1")]
        Task<LeagueRoot> GetFullLeagueDetails([Path] int leagueId, [Path] int year);

        [Get("{year}/export?TYPE=leagueStandings&L={leagueId}&APIKEY={ApiKey}&JSON=1")]
        Task<MflStandingsParent> GetStandings([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);
        
        [Get("{year}/export?TYPE=pendingTrades&L={leagueId}&APIKEY={ApiKey}&FRANCHISE_ID={franchiseNum}&JSON=1")]
        Task<MflPendingTradesListRoot> GetPendingTrades([Path] int leagueId, [Path] string franchiseNum, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=freeAgents&L={leagueId}&APIKEY={ApiKey}&JSON=1")]
        Task<MflAgentsParent> GetFreeAgents([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);
        
        [Get("{year}/export?TYPE=salaries&L={leagueId}&APIKEY={ApiKey}&JSON=1")]
        Task<MflSalariesParent> GetSalaries([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);
        
        [Get("{year}/export?TYPE=playerProfile&P={ids}&JSON=1")]
        Task<MflPlayerProfilesRoot> GetPlayerDetails([Path] string ids, [Path] int year);
        
        [Get("{year}/export?TYPE=transactions&L={leagueId}&APIKEY={ApiKey}&W=&TRANS_TYPE=TRADE&FRANCHISE=&DAYS=1&COUNT=&JSON=1")]
        Task<TradeTransactionMulti> GetRecentTrade([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=tradeBait&L={leagueId}&APIKEY={ApiKey}&INCLUDE_DRAFT_PICKS=true&JSON=1")]
        Task<TradeBaitsParent> GetTradeBait([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);
        
        [Get("{year}/export?TYPE=players&L={leagueId}&APIKEY={ApiKey}&DETAILS=&SINCE=&PLAYERS={player}&JSON=1")]
        Task<PlayerParent> GetBotPlayerDetails([Path] int leagueId, [Path] string player, [Path] int year, [Path] string ApiKey = Utils.ApiKey);
        
        [Get("{year}/export?TYPE=players&L={leagueId}&APIKEY={ApiKey}&DETAILS=&SINCE=&PLAYERS={players}&JSON=1")]
        Task<PlayersParent> GetBotPlayersDetails([Path] int leagueId, [Path] string players, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=rosters&L={leagueId}&APIKEY=&FRANCHISE=&W=&JSON=1")]
        Task<MflRostersRoot> GetRostersWithContracts([Path] int leagueId, [Path] int year);

        [Get("{year}/export?TYPE=liveScoring&L={leagueId}&APIKEY=&W={weekNum}&DETAILS=&JSON=1")]
        Task<LiveScoreRoot> GetLiveScores([Path] int leagueId, [Path] string weekNum, [Path] int year);

        [Get("{year}/export?TYPE=projectedScores&L={leagueId}&APIKEY={ApiKey}&W={weekNum}&PLAYERS=&POSITION=&STATUS=&COUNT=&JSON=1")]
        Task<ProjectedScoresRoot> GetProjections([Path] int leagueId, [Path] string weekNum, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=schedule&L={leagueId}&APIKEY={ApiKey}&W=&F=&JSON=1")]
        Task<ScheduleRoot> GetMatchupSchedule([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=players&L={leagueId}&APIKEY={ApiKey}&DETAILS=&SINCE=&PLAYERS=&JSON=1")]
        Task<PlayersParent> GetAllMflPlayers([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=assets&L={leagueId}&APIKEY={ApiKey}&JSON=1")]
        Task<MflAssetsRoot> GetFranchiseAssets([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);

        [Get("{year}/export?TYPE=playerScores&L={leagueId}&APIKEY={ApiKey}&W=AVG&YEAR=&PLAYERS=&POSITION=&STATUS=&RULES=&COUNT=&JSON=1")]
        Task<MflAverageScoreRoot> GetAveragePlayerScores([Path] int leagueId, [Path] int year, [Path] string ApiKey = Utils.ApiKey);
        [Get("{year}/export?TYPE=draftResults&L={leagueId}&APIKEY={ApiKey}&JSON=1")]
        Task<MflDraftResultRoot> GetMflDraftResults([Path] int leagueId, [Path] int year,  [Path] string ApiKey = Utils.ApiKey);
    }
    
}
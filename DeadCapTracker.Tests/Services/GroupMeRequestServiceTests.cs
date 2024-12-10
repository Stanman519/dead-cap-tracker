using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Repositories;
using DeadCapTracker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeadCapTracker.Tests.Services
{
    public class GroupMeRequestServiceTests
    {
        private readonly Mock<IMflTranslationService> _mflTranslationServiceMock;
        private readonly Mock<IDataSetHelperService> _dataHelperMock;
        private readonly Mock<IGroupMePostRepo> _gmMock;
        private readonly Mock<ILeagueService> _leagueServiceMock;
        private readonly Mock<IRumorService> _rumorMock;
        private readonly Mock<IInsultApi> _insultMock;
        private readonly Mock<ILogger<GroupMeRequestService>> _loggerMock;
        private readonly Mock<DeadCapTrackerContext> _dbMock;
        private readonly GroupMeRequestService _service;

        public GroupMeRequestServiceTests()
        {
            _mflTranslationServiceMock = new Mock<IMflTranslationService>();
            _dataHelperMock = new Mock<IDataSetHelperService>();
            _gmMock = new Mock<IGroupMePostRepo>();
            _leagueServiceMock = new Mock<ILeagueService>();
            _rumorMock = new Mock<IRumorService>();
            _insultMock = new Mock<IInsultApi>();
            _loggerMock = new Mock<ILogger<GroupMeRequestService>>();
            _dbMock = new Mock<DeadCapTrackerContext>();

            _service = new GroupMeRequestService(
                _mflTranslationServiceMock.Object,
                _dataHelperMock.Object,
                _gmMock.Object,
                _leagueServiceMock.Object,
                _rumorMock.Object,
                _dbMock.Object,
                _insultMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task PostStandingsToGroup_ShouldPostStandings()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var year = 2023;
            var standingsData = new List<StandingsV2>
            {
                new StandingsV2
                {
                    FranchiseId = 1,
                    TeamStandings = new List<AnnualScoringData>
                    {
                        new AnnualScoringData
                        {
                            FranchiseId = 1,
                            Year = year,
                            PointsFor = 100,
                            H2hWins = 10,
                            H2hLosses = 2,
                            VictoryPoints = 20
                        }
                    }
                }
            };

            _leagueServiceMock.Setup(x => x.GetStandingsV2(leagueId, year)).ReturnsAsync(standingsData);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.PostStandingsToGroup(botId, leagueId, year);

            // Assert
            Assert.NotNull(result);
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Exactly(2));
        }

        [Fact]
        public async Task PostTradeOffersToGroup_ShouldPostTradeOffers()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var year = 2023;
            var trades = new List<PendingTradeDTO>
            {
                new PendingTradeDTO
                {
                    offeredTo = 1,
                    timeStamp = DateTime.Now
                }
            };

            _leagueServiceMock.Setup(x => x.FindPendingTrades(leagueId, year)).ReturnsAsync(trades);
            _gmMock.Setup(x => x.GetMemberIds(It.IsAny<string>())).ReturnsAsync(new GroupParent { response = new GroupModel { members = new List<MemberModel> { new MemberModel { user_id = "1", nickname = "User1" } } } });
            _gmMock.Setup(x => x.BotPostWithTag(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.PostTradeOffersToGroup(botId, leagueId, year, "asdfasdf");

            // Assert
            Assert.NotNull(result);
            _gmMock.Verify(x => x.BotPostWithTag(botId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task PostCompletedTradeToGroup_ShouldPostCompletedTrade()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var tradeInfoList = new List<TradeSingle>
            {
                new TradeSingle
                {
                    franchise = "1",
                    franchise2 = "2",
                    timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString()
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetCompletedTrades(leagueId)).ReturnsAsync(tradeInfoList);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostCompletedTradeToGroup(botId, leagueId);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task PostTradeRumor_ShouldPostTradeRumor()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var baitList = new List<TradeBait>
            {
                new TradeBait
                {
                    inExchangeFor = "Player1"
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetNewTradeBait(leagueId)).ReturnsAsync(baitList);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostTradeRumor(botId, leagueId);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task FindAndPostContract_ShouldPostContract()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var year = 2023;
            var nameSearch = "Player1";
            var hits = new List<RosterPlayer>
            {
                new RosterPlayer
                {
                    name = "Player1",
                    salary = "100",
                    contractYear = "1",
                    owner = "Owner1"
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetRosteredPlayersByName(leagueId, year, nameSearch)).ReturnsAsync(hits);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.FindAndPostContract(botId, leagueId, year, nameSearch);

            // Assert
            Assert.NotNull(result);
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task CheckLineupsForHoles_ShouldCheckLineups()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var thisWeek = "1";
            var liveScores = new List<LiveScoreFranchise>
            {
                new LiveScoreFranchise
                {
                    id = "1",
                    players = new LiveScoringPlayers
                    {
                        player = new List<LiveScorePlayer>
                        {
                            new LiveScorePlayer
                            {
                                id = "1",
                                status = "starter",
                                gameSecondsRemaining = "3600"
                            }
                        }
                    }
                }
            };
            var groupParent = new GroupParent { response = new GroupModel { members = new List<MemberModel> { new MemberModel { user_id = "1", nickname = "User1" } } } };
            _mflTranslationServiceMock.Setup(x => x.GetThisLeagueWeek(leagueId)).ReturnsAsync(thisWeek);
            _mflTranslationServiceMock.Setup(x => x.GetLiveScoresForFranchises(leagueId, thisWeek)).ReturnsAsync(liveScores);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
            _gmMock.Setup(x => x.GetMemberIds(It.IsAny<string>())).ReturnsAsync(groupParent);
            // Act
            await _service.CheckLineupsForHoles(botId, leagueId, "asdfasdf");

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }
        // fix the below test           


        [Fact]
        public async Task PostCapSpace_ShouldPostCapSpace()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var salaries = new List<FranchiseRoster>
            {
                new FranchiseRoster
                {
                    week = "3",
                    player = new List<RosterPlayer>
                    {
                        new RosterPlayer
                        {
                            contractYear = "1",
                            id = "1",
                            salary = "100", name = "Player1", owner = "Owner1", status = "ROSTER"
                        },
                        new RosterPlayer
                        {
                                                        contractYear = "2",
                            id = "21",
                            salary = "110", name = "Player2", owner = "Owner1", status = "ROSTER"
                        }
                    },
                    id = "1"
                }
            };
            var adjustments = new List<DeadCapData>
            {
                new DeadCapData(1, "hello")
            };

            _mflTranslationServiceMock.Setup(x => x.GetFranchiseSalaries(leagueId)).ReturnsAsync(salaries);
            _leagueServiceMock.Setup(x => x.GetDeadCapData()).ReturnsAsync(adjustments);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostCapSpace(botId, leagueId);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task FindAndPostLiveScores_ShouldPostLiveScores()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var thisWeek = "1";
            var matchups = new List<Matchup>
            {
                new Matchup
                {
                    franchise = new List<LiveScoreFranchise>
                    {
                        new LiveScoreFranchise
                        {
                            
                        },
                        new LiveScoreFranchise
                        {
                            id = "2",
                            score = "90"
                        }
                    }
                }
            };
            var projections = new List<ProjectedPlayerScore>
            {
                new ProjectedPlayerScore
                {
                    id = "1",
                    score = "110"
                },
                new ProjectedPlayerScore
                {
                    id = "2",
                    score = "95"
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetThisLeagueWeek(leagueId)).ReturnsAsync(thisWeek);
            _mflTranslationServiceMock.Setup(x => x.GetLiveScoresForMatchups(leagueId, thisWeek)).ReturnsAsync(matchups);
            _mflTranslationServiceMock.Setup(x => x.GetProjections(leagueId, thisWeek)).ReturnsAsync(projections);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.FindAndPostLiveScores(botId, leagueId);

            // Assert
            Assert.NotNull(result);
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task PostFranchiseTagAmounts_ShouldPostFranchiseTagAmounts()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var year = DateTime.UtcNow.Year - 1;
            var salaries = new List<MflPlayer>
            {
                new MflPlayer
                {
                    Id = "1",
                    Salary = "100"
                }
            };
            var positions = new List<Player>
            {
                new Player
                {
                    id = "1",
                    position = "QB"
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetAllSalaries(leagueId)).ReturnsAsync(salaries);
            _mflTranslationServiceMock.Setup(x => x.GetAllRelevantPlayers(leagueId)).ReturnsAsync(positions);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostFranchiseTagAmounts(botId, leagueId);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task OptimizeLineup_ShouldOptimizeLineup()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var gmId = "gmId";
            var mflFranchiseId = "0001";
            var thisWeek = "1";
            var liveScores = new List<LiveScoreFranchise>
            {
                new LiveScoreFranchise
                {
                    id = mflFranchiseId,
                    players = new LiveScoringPlayers
                    {
                        player = new List<LiveScorePlayer>
                        {
                            new LiveScorePlayer
                            {
                                id = "1",
                                status = "starter",
                                gameSecondsRemaining = "3600"
                            }
                        }
                    }
                }
            };
            var projections = new List<ProjectedPlayerScore>
            {
                new ProjectedPlayerScore
                {
                    id = "1",
                    score = "110"
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetThisLeagueWeek(leagueId)).ReturnsAsync(thisWeek);
            _mflTranslationServiceMock.Setup(x => x.GetLiveScoresForFranchises(leagueId, "")).ReturnsAsync(liveScores);
            _mflTranslationServiceMock.Setup(x => x.GetProjections(leagueId, thisWeek)).ReturnsAsync(projections);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.OptimizeLineup(botId, leagueId, gmId);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task PostTopUpcomingFreeAgents_ShouldPostTopUpcomingFreeAgents()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var positionRequest = "QB";
            var nextYearAsDefault = DateTime.UtcNow.Year + 1;
            var avgPts = new List<PlayerAvgScore>
            {
                new PlayerAvgScore
                {
                    id = "1",
                    score = "100"
                }
            };
            var salaries = new List<MflPlayer>
            {
                new MflPlayer
                {
                    Id = "1",
                    Salary = "100"
                }
            };
            var players = new List<Player>
            {
                new Player
                {
                    id = "1",
                    position = "QB"
                }
            };

            _mflTranslationServiceMock.Setup(x => x.GetAveragePlayerScores(leagueId, nextYearAsDefault - 1)).ReturnsAsync(avgPts);
            _mflTranslationServiceMock.Setup(x => x.GetAllSalaries(leagueId)).ReturnsAsync(salaries);
            _mflTranslationServiceMock.Setup(x => x.GetAllRelevantPlayers(leagueId)).ReturnsAsync(players);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostTopUpcomingFreeAgents(botId, leagueId, positionRequest, nextYearAsDefault);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task PostDraftProjections_ShouldPostDraftProjections()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var year = 2023;
            var standings = new List<MflFranchiseStandings>
            {
                new MflFranchiseStandings
                {
                    id = "1",
                    h2hw = "10",
                    h2hl = "2"
                }
            };
            var draftPicks = new List<MflAssetsFranchise>
            {
                new MflAssetsFranchise
                {
                    id = "1",
                    currentYearDraftPicks = new FutureYearDraftPicks
                    {

                            draftPick = new List<DraftPick>
                            {
                                new DraftPick
                                {
                                    description = "test pick",
                                    pick = "1",

                                }
                            }

                    }
            }};

            _mflTranslationServiceMock.Setup(x => x.GetFranchiseStandings(leagueId)).ReturnsAsync(standings);
            _mflTranslationServiceMock.Setup(x => x.GetFranchiseAssets(leagueId)).ReturnsAsync(draftPicks);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostDraftProjections(botId, leagueId, year);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task PostFutureDeadCap_ShouldPostFutureDeadCap()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;
            var deadCapData = new List<DeadCapData>
            {
                new DeadCapData(1234, "Ryan Stanley")
            };

            _leagueServiceMock.Setup(x => x.GetDeadCapData()).ReturnsAsync(deadCapData);
            _gmMock.Setup(x => x.BotPost(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            // Act
            await _service.PostFutureDeadCap(botId, leagueId);

            // Assert
            _gmMock.Verify(x => x.BotPost(botId, It.IsAny<string>(), false), Times.Once);
        }

        [Fact]
        public async Task PostDraftBudgets_ShouldPostDraftBudgets()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 1;

            _leagueServiceMock.Setup(x => x.MapPickBudgetToOwners(leagueId)).Returns(Task.CompletedTask);

            // Act
            await _service.PostDraftBudgets(botId, leagueId);

            // Assert
            _leagueServiceMock.Verify(x => x.MapPickBudgetToOwners(leagueId), Times.Once);
        }

    }
}

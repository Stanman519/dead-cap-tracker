using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GroupMeRequestServicePostStandingsTests
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

        public GroupMeRequestServicePostStandingsTests()
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

            // Mocking static dictionaries
            Utils.owners = new Dictionary<int, Dictionary<int, string>>
            {
                { 1, new Dictionary<int, string> { { 1, "Owner1" }, { 2, "Owner2" } } }
            };
            Utils.memberIds = new Dictionary<int, Dictionary<int, string>>
            {
                { 1, new Dictionary<int, string> { { 1, "Member1" }, { 2, "Member2" } } }
            };
        }

        [Fact]
        public async Task PostStandingsToGroup_ShouldPostStandings()
        {
            // Arrange
            var botId = "botId";
            var leagueId = 13894;
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
                        },
                        new AnnualScoringData
                        {
                            FranchiseId = 2,
                            Year = year,
                            PointsFor = 90,
                            H2hWins = 9,
                            H2hLosses = 3,
                            VictoryPoints = 18
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
            Assert.Equal(2, result.Count);
            _gmMock.Verify(x => x.BotPost(botId, It.Is<string>(s => s.Contains("STANDINGS")), false), Times.Once);
            _gmMock.Verify(x => x.BotPost(botId, It.Is<string>(s => s.Contains("Tri-Year Trophy Presented by Taco Bell")), false), Times.Once);
        }
    }
}

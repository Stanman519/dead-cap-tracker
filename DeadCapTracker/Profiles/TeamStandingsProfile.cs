using AutoMapper;
using DeadCapTracker.Models;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;

namespace DeadCapTracker.Profiles
{
    public class TeamStandingsProfile : Profile
    {
        public TeamStandingsProfile()
        {
            CreateMap<TeamStandingsDTO, TeamStandings>();
        }
    }
}
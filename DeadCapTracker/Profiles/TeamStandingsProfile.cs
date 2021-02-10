using AutoMapper;
using DeadCapTracker.Models;

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
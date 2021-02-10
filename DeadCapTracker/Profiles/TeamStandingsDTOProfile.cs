using AutoMapper;
using DeadCapTracker.Models.MFL;

namespace DeadCapTracker.Profiles
{
    public class TeamStandingsDTOProfile : Profile
    {

        public TeamStandingsDTOProfile()
        {
            CreateMap<MflFranchiseStandings, TeamStandingsDTO>()
                .ForMember(dest => dest.FranchiseId,
                    opts => opts.MapFrom(src => src.id))
                .ForMember(dest => dest.PointsFor,
                    opts => opts.MapFrom(src => src.pf))
                .ForMember(dest => dest.H2hWins, opts =>
                    opts.MapFrom(src => src.h2hw))
                .ForMember(dest => dest.H2hLosses, opts =>
                    opts.MapFrom(src => src.h2hl))
                .ForMember(dest => dest.VictoryPoints, opts =>
                    opts.MapFrom(src => src.vp))
                .ForMember(dest => dest.AllPlayWins,
                    opts => opts.MapFrom(src => src.all_play_w))
                .ForMember(dest => dest.AllPlayLosses,
                    opts => opts.MapFrom(src => src.all_play_l))
                .ForMember(dest => dest.AllPlayTies,
                    opts => opts.MapFrom(src => src.all_play_t));
        }
        
    }
}
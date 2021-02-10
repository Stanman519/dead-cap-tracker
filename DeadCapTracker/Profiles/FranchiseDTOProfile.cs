using DeadCapTracker.Models.MFL;
using AutoMapper;
using DeadCapTracker.Models;

namespace DeadCapTracker.Profiles
{
    public class FranchiseDTOProfile : Profile
    {
        public FranchiseDTOProfile()
        {
            CreateMap<MflFranchise, FranchiseDTO>()
                .ForMember(dest => dest.FranchiseId,
                    opts => opts.MapFrom(src => src.Id))
                .ForMember(dest => dest.Teamname,
                    opts => opts.MapFrom(src => src.Name))
                .ForMember(dest => dest.Abbrev, opts =>
                    opts.MapFrom(src => string.IsNullOrEmpty(src.Abbrev) ? "" : src.Abbrev))
                .ForMember(dest => dest.Icon, opts =>
                    opts.MapFrom(src => string.IsNullOrEmpty(src.Icon) ? "" : src.Icon))
                .ForMember(dest => dest.Ownername, opts =>
                    opts.MapFrom(src => string.IsNullOrEmpty(src.Owner_name) ? "" : src.Owner_name));

            
        }
    }
}
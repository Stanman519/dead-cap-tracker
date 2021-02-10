using AutoMapper;
using DeadCapTracker.Models.MFL;

namespace DeadCapTracker.Profiles
{
    public class PlayerDetailsDTOProfile : Profile
    {
        public PlayerDetailsDTOProfile()
        {
            CreateMap<MflPlayerProfile, PlayerDetailsDTO>()

                .ForMember(d => d.Age,
                    opts => opts.MapFrom(s => s.player.age))
                .ForMember(d => d.Height,
                    opts => opts.MapFrom(s => s.player.height))
                .ForMember(d => d.Id,
                    opts => opts.MapFrom(s => s.player.id))
                .ForMember(d => d.Weight,
                    opts => opts.MapFrom(s => s.player.weight))
                .ForMember(d => d.DOB,
                    opts => opts.MapFrom(s => s.player.dob))
                .ForMember(d => d.Age,
                    opts => opts.MapFrom(s => s.player.age))
                .ForMember(dest => dest.Position,
                    opts => opts.MapFrom(source => SpaceSplit(source.Name)[SpaceSplit(source.Name).Length - 1]))
                .ForMember(dest => dest.Team,
                    opts => opts.MapFrom(source => SpaceSplit(source.Name)[SpaceSplit(source.Name).Length - 2]))
                .ForMember(dest => dest.Name, opts =>
                    opts.MapFrom(src => parseName(src.Name)));
        }

        
        public string[] SpaceSplit(string str)
        {
            return str.Split(" ");
        }
        public string parseName(string description)
        {
            string[] commaSplit = description.Split(",");
            string[] spaceSplit = commaSplit[1].Split(" ");
            string lastName = commaSplit[0];
            return spaceSplit[1] + " " + lastName;
        }
        
    }
}
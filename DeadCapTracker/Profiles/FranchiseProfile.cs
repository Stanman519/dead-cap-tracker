using AutoMapper;
using DeadCapTracker.Models;

namespace DeadCapTracker.Profiles
{
    public class FranchiseProfile : Profile
    {
        public FranchiseProfile()
        {
            CreateMap<FranchiseDTO, Franchise>();
        }
    }
}
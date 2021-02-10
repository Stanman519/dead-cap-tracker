using AutoMapper;
using DeadCapTracker.Models;

namespace DeadCapTracker.Profiles
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<TransactionDTO, Transaction>();
        }
    }
}
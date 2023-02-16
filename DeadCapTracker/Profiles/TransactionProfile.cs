using AutoMapper;
using DeadCapTracker.Models;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.DTOs;

namespace DeadCapTracker.Profiles
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, TransactionDTO>();
            CreateMap<TransactionDTO, Transaction>()
             .ForMember(dest => dest.Leagueid,
                 opts => opts.MapFrom(src => src.LeagueId))
                // .ForMember(dest => dest.Transactionid,
                // opts => opts.MapFrom(src => src.TransactionId))
                // .ForMember(dest => dest.Timestamp,
                //     opts => opts.MapFrom(src => src.Timestamp))
                // .ForMember(dest => dest.Franchiseid,
                //     opts => opts.MapFrom(src => src.FranchiseId))
                // .ForMember(dest => dest.Salary,
                //     opts => opts.MapFrom(src => src.Salary))
                // .ForMember(dest => dest.Amount,
                //     opts => opts.MapFrom(src => src.Amount))
                // .ForMember(dest => dest.Playername,
                //     opts => opts.MapFrom(src => src.PlayerName))
                // .ForMember(dest => dest.Position,
                //     opts => opts.MapFrom(src => src.Position))
                // .ForMember(dest => dest.Team,
                //     opts => opts.MapFrom(src => src.Team))
                // .ForMember(dest => dest.Years,
                //     opts => opts.MapFrom(src => src.Years))
                // .ForMember(dest => dest.Yearoftransaction,
                //     opts => opts.MapFrom(src => src.YearOfTransaction))
                ;
        }
    }
}
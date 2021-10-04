using System;
using AutoMapper;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;

namespace DeadCapTracker.Profiles
{
    public class PendingTradeDTOProfile : Profile
    {
        public PendingTradeDTOProfile()
        {
            CreateMap<MflPendingTrade, PendingTradeDTO>()
                .ForMember(destination => destination.tradeId,
                    opts => opts.MapFrom(src => src.trade_id))
                .ForMember(d => d.offeredTo,
                    opts => opts.MapFrom(src => src.offeredTo))
                .ForMember(d => d.offeringTeam,
                    opts => opts.MapFrom(src => src.offeringTeam))
                .ForMember(d => d.comments,
                    opts => opts.MapFrom(src => src.comments))
                .ForMember(d => d.description,
                    opts => opts.MapFrom(src => src.description))
                .ForMember(d => d.willReceive,
                    opts => opts.MapFrom(src => src.will_receive))
                .ForMember(d => d.willGiveUp,
                    opts => opts.MapFrom(src => src.will_give_up))
                .ForMember(d => d.timeStamp, opts =>
                    opts.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds(int.Parse(src.timestamp)).DateTime))
                .ForMember(d => d.expires, opts =>
                    opts.MapFrom(src => DateTimeOffset.FromUnixTimeSeconds(int.Parse(src.expires)).DateTime));
        }
    }
}
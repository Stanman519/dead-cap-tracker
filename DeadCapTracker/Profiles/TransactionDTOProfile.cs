using System;
using System.Transactions;
using AutoMapper;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;


namespace DeadCapTracker.Profiles
{
    public class TransactionDTOProfile : Profile
    {
        public TransactionDTOProfile()
        {
            CreateMap<MflSalaryAdjustment, TransactionDTO>()
                
                .ForMember(destination => destination.TransactionId,
                    opts => opts.MapFrom(source => source.Id))
                .ForMember(destination => destination.FranchiseId,
                    opts => opts.MapFrom(source => source.Franchise_Id))
                .ForMember(dest => dest.Position,
                    opts => opts.MapFrom(source => SpaceSplit(CommaSplitDescription(source.Description)[1])[3]))
                // .ForMember(dest => dest.Salary, opts => 
                //     opts.MapFrom(src => 
                //         //commsplit[1] has first name, team, position, salary
                //         moneySplit(SpaceSplit(CommaSplitDescription(src.Description)[1])[SpaceSplit(CommaSplitDescription(src.Description)[1]).Length - 1])))
                .ForMember(dest => dest.PlayerName, opts => 
                    opts.MapFrom(src => parseName(src.Description)))
                .ForMember(dest => dest.Team, opts => 
                    opts.MapFrom(src => SpaceSplit(CommaSplitDescription(src.Description)[1])[2]))
                .ForMember(dest => dest.Years, opts => 
                    opts.MapFrom(source => source.Description.Substring(source.Description.Length - 2,
                        1)))
                .ForMember(dest => dest.Timestamp, opts =>
                    opts.MapFrom(source => DateTimeOffset.FromUnixTimeSeconds(int.Parse(source.Timestamp)).DateTime));

          

        }
        public string[] CommaSplitDescription(string str)
        {
            return str.Split(",");   
        }

        public string[] SpaceSplit(string str)
        {
            return str.Split(" ");
        }

        public string moneySplit(string str)
        {
            string[] x = str.Split("$");
            return x[x.Length - 1];
        }

        public string parseName(string description)
        {
            string[] commaSplit = description.Split(",");
            string[] spaceSplit = commaSplit[1].Split(" ");
            string lastName = commaSplit[0].Split(" ")[1];
            return spaceSplit[1] + " " + lastName;
        }
        
        
    }
}
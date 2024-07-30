using System;
using System.Collections.Generic;
using System.Linq;
using DeadCapTracker.Models.BotModels;
using DeadCapTracker.Models.MFL;

namespace DeadCapTracker.Services
{
    public interface IDataSetHelperService
    {
        List<FranchiseCapSummary> CreateFranchiseCapSummaries(List<FranchiseRoster> rosters, List<DeadCapData> adjustments);
    }

    public class DataSetHelperService : IDataSetHelperService
    {

        public DataSetHelperService()
        {
        }
        
        public List<FranchiseCapSummary> CreateFranchiseCapSummaries(List<FranchiseRoster> rosters, List<DeadCapData> adjustments)
        {
            var _thisYear = DateTime.Now.Year;
            var salarySummaries = new List<FranchiseCapSummary>();
                        rosters.ForEach(roster =>
            {
                var rosterBoys = roster.player.Where(p => p.status == "ROSTER").Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                var irBoys = roster.player.Where(p => p.status == "INJURED_RESERVE").Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                var taxiBoys = roster.player.Where(p => p.status == "TAXI_SQUAD").Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                var nextYearBoys = roster.player.Where(p => Int32.Parse(p.contractYear) > 1).Select(_ => new PlayerSalaryDTO
                {
                    Id = _.id,
                    ContractYear = Int32.Parse(_.contractYear),
                    Salary = _.status == "TAXI_SQUAD" ? (decimal)0.2 * Decimal.Parse(_.salary) : Decimal.Parse(_.salary),
                    Status = _.status
                }).ToList();
                salarySummaries.Add(new FranchiseCapSummary
                {
                    
                    Id = Int32.Parse(roster.id),
                    CurrentRosterSalary = rosterBoys.Sum(_ => _.Salary),
                    CurrentIRSalary = ((decimal) 0.5 * irBoys.Sum(_ => _.Salary)),
                    CurrentTaxiSalary = ((decimal) 0.2 * taxiBoys.Sum(_ => _.Salary)),
                    DeadCapData = adjustments.FirstOrDefault(_ => _.FranchiseId == Int32.Parse(roster.id))?.Amount ?? new Dictionary<string, decimal>(),
                    NextYearRosterSalary = nextYearBoys.Sum(_ => _.Salary)
                });
            });
            return salarySummaries.OrderByDescending(tm =>
                tm.CurrentRosterSalary + tm.CurrentTaxiSalary + tm.CurrentIRSalary +
                (tm.DeadCapData.ContainsKey(_thisYear.ToString()) ? tm.DeadCapData[_thisYear.ToString()] : 0)).ToList();
        }
    }
}
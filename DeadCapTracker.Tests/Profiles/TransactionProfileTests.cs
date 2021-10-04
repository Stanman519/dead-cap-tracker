using AutoMapper;
using DeadCapTracker.Models.DTOs;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Profiles;
using Xunit;

namespace DeadCapTracker.Tests.Profiles
{
    public class TransactionProfileTests
    {
        public IMapper _mapper { get; private set; }

        public TransactionProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new TransactionDTOProfile());
            });
            _mapper = config.CreateMapper();    
        }
        
        [Fact]
        public void YearsAreCorrect()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.StrictEqual(3 , tDTO.Years);
        }
        [Fact]
        public void CheckTeamAndPositionAfterMap()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.Equal("CLE", tDTO.Team);
            Assert.Equal("WR", tDTO.Position);
        }
        
        [Fact]
        public void VerifySalaryIsParsed()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.StrictEqual(100, tDTO.Salary);
        }
        [Fact]
        public void CheckNameStringAfterMap()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.Equal("Donovan Peoples-Jones", tDTO.PlayerName);
        }
        [Fact]
        public void VerifyAmountIsDecimal()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.StrictEqual(40, tDTO.Amount);
        }
        [Fact]
        public void YearOfTransactionIsCorrect()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.StrictEqual(2020, tDTO.YearOfTransaction);
        }
        [Fact]
        public void FranchiseIdIsCorrect()
        {
            //arrange
            var test = CreateTransaction();
            //act
            TransactionDTO tDTO = _mapper.Map<TransactionDTO>(test);
            //assert
            Assert.StrictEqual(11, tDTO.FranchiseId);
        }
        

        private MflTransaction CreateTransaction()
        {
            MflTransaction testMflTransaction = new MflTransaction();
            testMflTransaction.Timestamp = "1601470800";
            testMflTransaction.Id = "12";
            testMflTransaction.Franchise_Id = "0011";
            testMflTransaction.Amount = "40";
            testMflTransaction.Description = "Dropped Peoples-Jones, Donovan CLE WR (Salary: $100, years left: 3)";
            testMflTransaction.YearOfTransaction = 2020;
            return testMflTransaction;
        }
    }
}
using System;
using AutoMapper;
using DeadCapTracker.Models.MFL;
using DeadCapTracker.Profiles;
using Xunit;

namespace DeadCapTracker.Tests.Profiles
{
    public class FranchiseProfileTests
    {
        public IMapper _mapper { get; private set; }

        public FranchiseProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new FranchiseDTOProfile());
            });
            _mapper = config.CreateMapper();    
        }
        
        [Fact]
        public void BidBalanceIsDecimal()
        {
            //arrange
            var test = CreateTestFranchise();
            //act
            var fDTO = _mapper.Map<FranchiseDTO>(test);
            //assert
            Assert.StrictEqual(Convert.ToDecimal(60.50) , fDTO.BBidAvailableBalance);
        }
        
        [Fact]
        public void TeamIdIsInt()
        {
            //arrange
            var test = CreateTestFranchise();
            //act
            var fDTO = _mapper.Map<FranchiseDTO>(test);
            //assert
            Assert.StrictEqual(3 , fDTO.FranchiseId);
        }
        
        [Fact]
        public void OwnernameIsEmpty()
        {
            //arrange
            var test = CreateTestFranchise();
            //act
            var fDTO = _mapper.Map<FranchiseDTO>(test);
            //assert
            Assert.Equal("" , fDTO.Ownername);
        }
        
        public MflFranchise CreateTestFranchise()
        {
            MflFranchise testMflFranchise = new MflFranchise();
            testMflFranchise.Icon = "https://www64.myfantasyleague.com/fflnetdynamic2020/13894_franchise_icon0003.png";
            testMflFranchise.BBidAvailableBalance = "60.50";
            testMflFranchise.Name = "Little Dumpster Fires Everywhere";
            testMflFranchise.Id = "0003";
            return testMflFranchise;
        }
    }
}
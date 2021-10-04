using AutoMapper;
using DeadCapTracker.Profiles;
using DeadCapTracker.Repositories;

namespace DeadCapTracker.Tests.Services
{
    public class TransactionServiceTests
    {
        public IMflApi _api;
        public IMapper _mapper { get; private set; }

        public TransactionServiceTests()
        {
            //_transactionService = transactionService;
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new TransactionDTOProfile());
            });
            _mapper = config.CreateMapper();
        }
    }
}

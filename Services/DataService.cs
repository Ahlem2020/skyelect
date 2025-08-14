using MongoDB.Driver;
using ElectionApi.Repositories;

namespace ElectionApi.Services
{
    public interface IDataService
    {
        IMongoDatabase Database { get; }
        ICandidateRepository Candidates { get; }
        IOfficeRepository Offices { get; }
        IVoteRepository Votes { get; }
        ICountryRepository Countries { get; }
    }

    public class DataService : IDataService
    {
        private readonly IMongoContext _dbContext;

        public DataService(IMongoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IMongoDatabase Database => _dbContext.Database;
        public ICandidateRepository Candidates => new CandidateRepository(_dbContext);
        public IOfficeRepository Offices => new OfficeRepository(_dbContext);
        public IVoteRepository Votes => new VoteRepository(_dbContext);
        public ICountryRepository Countries => new CountryRepository(_dbContext);
    }
}

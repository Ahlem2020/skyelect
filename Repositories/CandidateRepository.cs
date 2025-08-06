using MongoDB.Driver;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public class CandidateRepository : Repository<Candidate>, ICandidateRepository
    {
        public CandidateRepository(IMongoDatabase database) : base(database)
        {
        }
    }

    public interface ICandidateRepository : IRepository<Candidate> { }
}

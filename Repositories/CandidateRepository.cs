using MongoDB.Driver;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public class CandidateRepository : Repository<Candidate>, ICandidateRepository
    {
        public CandidateRepository(IMongoContext context) : base(context.Database)
        {
        }
    }

    public interface ICandidateRepository : IRepository<Candidate> { }
}

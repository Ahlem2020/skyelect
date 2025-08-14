using MongoDB.Bson;
using MongoDB.Driver;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public class VoteRepository : Repository<Vote>, IVoteRepository
    {
        public VoteRepository(IMongoContext context) : base(context.Database)
        {
        }
        public virtual Task<List<BsonDocument>> CountByCandidateAsync(FilterDefinition<Vote> filterExpression)
        {
            var groupby = new BsonDocument
            {
                {
                    "_id", new BsonDocument
                    {
                        { "candidateId", "$CandidateId"},
                    }
                },
                { "totalVote", new BsonDocument("$sum", "$TotalVote") }
            };

            return Task.Run(() =>
            {
                return _collection.Aggregate().Match(filterExpression)
                .Group(groupby)
                .ToListAsync();
            });
        }

        public virtual Task<List<BsonDocument>> CountByCandidateByHoureAsync(FilterDefinition<Vote> filterExpression)
        {
            var groupby = new BsonDocument
            {
                {
                    "_id", new BsonDocument 
                    {
                        { "candidateId", "$CandidateId"},
                        { "date", new BsonDocument("$dateToString", new BsonDocument{{"format", "%Y-%m-%d %H"}, {"date" , "$CreatedAt"}}) }
                    }
                },
                { "totalVote", new BsonDocument("$sum", "$TotalVote") }
            };

            return Task.Run(() =>
            {
                return _collection.Aggregate().Match(filterExpression)
                .Group(groupby)
                .ToListAsync();
            });
        }
        public virtual Task<List<BsonDocument>> CountByCandidateByMinuteAsync(FilterDefinition<Vote> filterExpression)
        {
            var groupby = new BsonDocument
    {
        {
            "_id", new BsonDocument
            {
                { "candidateId", "$CandidateId" },
                { "date", new BsonDocument("$dateToString", new BsonDocument
                    {
                        { "format", "%Y-%m-%d %H:%M" },  // ⬅️ Changed from "%H" to "%H:%M"
                        { "date", "$CreatedAt" }
                    })
                }
            }
        },
        { "totalVote", new BsonDocument("$sum", "$TotalVote") }
    };

            return _collection.Aggregate()
                .Match(filterExpression)
                .Group(groupby)
                .ToListAsync();
        }

    }

    public interface IVoteRepository : IRepository<Vote>
    {
        Task<List<BsonDocument>> CountByCandidateAsync(FilterDefinition<Vote> filterExpression);
        Task<List<BsonDocument>> CountByCandidateByHoureAsync(FilterDefinition<Vote> filterExpression);

        Task<List<BsonDocument>> CountByCandidateByMinuteAsync(FilterDefinition<Vote> filterExpression);
    }
}

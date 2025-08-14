using MongoDB.Driver;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public class OfficeRepository : Repository<Office>, IOfficeRepository
    {
        public OfficeRepository(IMongoContext context) : base(context.Database)
        {
        }
    }

    public interface IOfficeRepository : IRepository<Office> { }
}

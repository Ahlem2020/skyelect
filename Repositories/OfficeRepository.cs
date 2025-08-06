using MongoDB.Driver;
using ElectionApi.Models;

namespace ElectionApi.Repositories
{
    public class OfficeRepository : Repository<Office>, IOfficeRepository
    {
        public OfficeRepository(IMongoDatabase database) : base(database)
        {
        }
    }

    public interface IOfficeRepository : IRepository<Office> { }
}

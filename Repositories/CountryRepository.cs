using ElectionApi.Models;
using MongoDB.Driver;

namespace ElectionApi.Repositories
{
    public interface ICountryRepository : IRepository<Country>
    {
        Task<Country?> GetByNameAsync(string name);
    }

    public class CountryRepository : Repository<Country>, ICountryRepository
    {
        public CountryRepository(IMongoContext context) : base(context.Database)
        {
        }

        public async Task<Country?> GetByNameAsync(string name)
        {
            return await _collection.Find(c => c.Name == name).FirstOrDefaultAsync();
        }
    }
}

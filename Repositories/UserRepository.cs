using ElectionApi.Models;
using MongoDB.Driver;

namespace ElectionApi.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(IMongoContext context) : base(context.Database)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _collection.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _collection.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _collection.CountDocumentsAsync(u => u.Username == username) > 0;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _collection.CountDocumentsAsync(u => u.Email == email) > 0;
        }
    }
}

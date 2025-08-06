using Microsoft.Extensions.Options;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver;
using ElectionApi.Settings;

namespace ElectionApi
{
    public interface IMongoContext
    {
        IMongoClient Client { get; }
        IMongoDatabase Database { get; }
    }
    public class MongoContext : IMongoContext
    {
        private readonly MongoClient? _client;
        private readonly IMongoDatabase? _database;
        public MongoContext(IOptions<MongoDbSettings> dbOptions)
        {
            var settings = dbOptions.Value;
            
            try
            {
                // Use the connection string directly which includes authentication
                _client = new MongoClient(settings.ConnectionString);
                _database = _client.GetDatabase(settings.DatabaseName);
            }
            catch (Exception) { }
        }

        public IMongoClient Client => _client!;

        public IMongoDatabase Database => _database!;
    }
}

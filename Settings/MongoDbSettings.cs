namespace ElectionApi.Settings
{
    public interface IMongoDbSettings
    {
        string? ConnectionString { get; }
        string? Server {get;}
        int Port { get; }
        string? DatabaseName { get; }
        string? UserName { get; }
        string? Password { get; }
    }

    public class MongoDbSettings : IMongoDbSettings
    {
        public string? ConnectionString { get; set; }
        public string? Server { get; set; }
        public int Port { get; set; }
        public string? DatabaseName { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }
}

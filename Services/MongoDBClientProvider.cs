using MongoDB.Bson;
using MongoDB.Driver;
using PokeCord.Data;
using System.Security.Cryptography.X509Certificates;

public class MongoDBClientProvider
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<PlayerData> _playerDataCollection;

    public MongoDBClientProvider(string connectionString, string databaseName, string certificatePath)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        settings.UseTls = true;
        settings.SslSettings = new SslSettings
        {
            ClientCertificates = new List<X509Certificate>()
            {
                new X509Certificate2(certificatePath)
            }
        };

        _client = new MongoClient(settings);
        _database = _client.GetDatabase(databaseName);
        _playerDataCollection = _database.GetCollection<PlayerData>("PlayerData");
        var somedata = _playerDataCollection.Find(new BsonDocument()).ToListAsync();
        Console.WriteLine($"DATABASE: {somedata.Result}");
}

    public IMongoDatabase GetDatabase()
    {
        return _database;
    }
}

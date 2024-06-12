using MongoDB.Bson;
using MongoDB.Driver;
using PokeCord.Data;
using System.Security.Cryptography.X509Certificates;

public class MongoDBClientProvider
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<PlayerData> _playerDataCollection; // For testing

    public MongoDBClientProvider(string connectionString, string databaseName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        //settings.UseTls = true;

        _client = new MongoClient(settings);
        _database = _client.GetDatabase(databaseName);


        // Test connection
        _playerDataCollection = _database.GetCollection<PlayerData>("PlayerData");        
        try
        {
            var pingData = _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            Console.WriteLine($"Connected to MongoDB: {pingData}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"*****NOT CONNECTED to MongoDB: " + ex.Message );
        }
    }

    public IMongoDatabase GetDatabase()
    {
        return _database;
    }
}

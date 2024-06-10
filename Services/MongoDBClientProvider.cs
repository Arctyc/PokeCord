using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;

public class MongoDBClientProvider
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;

    public MongoDBClientProvider(string connectionString, string databaseName, string certificatePath)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        settings.UseTls = true;
        settings.SslSettings = new SslSettings { ClientCertificates = new[] { new X509Certificate2(certificatePath) } };

        _client = new MongoClient(settings);
        _database = _client.GetDatabase(databaseName);
    }

    public IMongoDatabase GetDatabase()
    {
        return _database;
    }
}
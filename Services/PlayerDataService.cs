using MongoDB.Driver;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace PokeCord.Services
{
    public class PlayerDataService
    {
        public const int pokeballRestockAmount = 40; // Amount of Pokeballs given per restock (currently daily)
        public const int currencyCap = 5000; // Max amount of pokemon dollars a player can have

        // MongoDB
        private readonly IMongoCollection<PlayerData> _playerDataCollection;

        public PlayerDataService(MongoDBClientProvider mongoDBClient)
        {
            var database = mongoDBClient.GetDatabase();
            _playerDataCollection = database.GetCollection<PlayerData>("PlayerData");
        }

        // Add player to MongoDB
        public async Task<bool> TryAddPlayerDataAsync(ulong userId, PlayerData playerData)
        {
            var existingPlayerData = await _playerDataCollection.Find(p => p._id == userId).FirstOrDefaultAsync();
            if (existingPlayerData != null)
            {
                Console.WriteLine($"Player data for userId: {userId} already exists.");
                return false;
            }

            try
            {
                await _playerDataCollection.InsertOneAsync(playerData); // Insert new player data
                return true;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"Failure to create new player:" + ex.Message);
                return false;
            }
        }

        // Update player in MongoDB
        public async Task<bool> TryUpdatePlayerDataAsync(ulong userId, PlayerData playerData)
        {
            try
            {
                var result = await _playerDataCollection.ReplaceOneAsync(p => p._id == userId, playerData);
                return result.ModifiedCount > 0;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"Error updating player data for userId: {userId}. Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TryUpdatePlayersAsync(UpdateDefinition<PlayerData> updateDefinition)
        {
            try
            {
                var updateResult = await _playerDataCollection.UpdateManyAsync(
                    Builders<PlayerData>.Filter.Empty, // Update all documents
                    updateDefinition);

                return updateResult.ModifiedCount > 0;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"Error updating players. Error: {ex.Message}");
                return false;
            }
        }

        // Get player from MongoDB
        public async Task<PlayerData> TryGetPlayerDataAsync(ulong userId)
        {
            try
            {
                PlayerData playerData = await _playerDataCollection.Find(p => p._id == userId).FirstOrDefaultAsync();
                return playerData;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"Error getting playerdata {ex.Message}");
                return null!;
            }
            
        }

        // Set all player's pokeballs to restock amount in MongoDB
        public async Task RestockPokeballsAsync(object? state)
        {
            var filter = Builders<PlayerData>.Filter.Lt(p => p.Pokeballs, pokeballRestockAmount);
            var update = Builders<PlayerData>.Update.Set(p => p.Pokeballs, pokeballRestockAmount);
            await _playerDataCollection.UpdateManyAsync(filter, update);

            Console.WriteLine("\n***Pokeballs have been reset for all players!");
        }

        // Get list of players sorted by experience from MongoDB
        public async Task<List<PlayerData>> GetLifetimeLeaderboardAsync()
        {
            List<PlayerData> players = await _playerDataCollection.Find(_ => true).SortByDescending(p => p.Experience).ToListAsync();
            Console.WriteLine($"Got Lifetime Leaderboard");
            return players;
            
        }

        // Get list of players sorted by weekly experience from MongoDB
        public async Task<List<PlayerData>> GetWeeklyLeaderboardAsync()
        {
            List<PlayerData> players = await _playerDataCollection.Find(_ => true).ToListAsync();
            Console.WriteLine($"Got Weekly Leaderboard");
            return players
                .Where(p => p.WeeklyExperience > 0)
                .OrderByDescending(p => p.WeeklyCaughtPokemon.Sum(pokemon => pokemon.BaseExperience ?? 0))
                .ToList();
        }

        // Push old save json data to mongodb. FIX: Remove when no-longer needed (MongoDB in-use)
        // Also remove unnecessary OldPlayerData class
        private readonly string _scoreboardFilePath = "scoreboard.json";
        private static ConcurrentDictionary<ulong, OldPlayerData> _scoreboard = new ConcurrentDictionary<ulong, OldPlayerData>();
        public async Task ConvertSaveDataToMongo()
        {
            if (!File.Exists(_scoreboardFilePath))
            {
                Console.WriteLine("Scoreboard data file not found. Cannot convert to MongoDB.");
                return; // Don't attempt to load from a non-existent file
            }

            try
            {
                await using var stream = File.OpenRead(_scoreboardFilePath);
                _scoreboard = await JsonSerializer.DeserializeAsync<ConcurrentDictionary<ulong, OldPlayerData>>(stream) ??
                    new ConcurrentDictionary<ulong, OldPlayerData>();
                Console.WriteLine($"Scoreboard loaded from {_scoreboardFilePath}");

                int countPushed = 0;
                // Handle playerData version mismatch
                foreach (var playerData in _scoreboard.Values)
                {
                    PlayerData newPlayerData = new PlayerData();
                    newPlayerData.Version = playerData.Version;
                    newPlayerData._id = playerData.UserId;
                    newPlayerData.UserName = playerData.UserName;
                    newPlayerData.Experience = playerData.Experience;
                    newPlayerData.WeeklyExperience = playerData.WeeklyExperience;
                    newPlayerData.TeamId = playerData.TeamId;
                    newPlayerData.Pokeballs = playerData.Pokeballs;
                    newPlayerData.PokemonDollars = playerData.PokemonDollars;
                    newPlayerData.CaughtPokemon = playerData.CaughtPokemon;
                    newPlayerData.WeeklyCaughtPokemon = playerData.WeeklyCaughtPokemon;
                    newPlayerData.EarnedBadges = playerData.EarnedBadges;
                    newPlayerData.PokeMartItems = playerData.PokeMartItems;

                    try
                    {
                        Console.WriteLine($"Adding {newPlayerData.UserName} to MongoDB...");
                        await TryAddPlayerDataAsync(newPlayerData._id, newPlayerData);
                        countPushed++;
                    }
                    catch (MongoException ex)
                    {
                        Console.WriteLine($"Failed to add {newPlayerData.UserName} to MongoDB: " + ex.Message );
                    }
                }
                Console.WriteLine($"{countPushed} of {_scoreboard.Count} players pushed to MongoDB");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading scoreboard data: {0}", ex.Message);
                _scoreboard = new ConcurrentDictionary<ulong, OldPlayerData>(); // Fallback to empty scoreboard on error
            }
        }

    }
}

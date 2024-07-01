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
            
            try
            {
                await Task.Delay(2000);// 2-second delay to avoid conflicts with database operation timing

                var filter = Builders<PlayerData>.Filter.Lt(p => p.Pokeballs, pokeballRestockAmount);
                var update = Builders<PlayerData>.Update.Set(p => p.Pokeballs, pokeballRestockAmount);

                // Execute the update
                var updateResult = await _playerDataCollection.UpdateManyAsync(filter, update);

                // Check if the operation was acknowledged by the server
                if (!updateResult.IsAcknowledged)
                {
                    Console.WriteLine($"{DateTime.Now} ***Pokeball restock operation was not acknowledged by the server.***");
                    return;
                }

                // Check if all matched documents were modified
                if (updateResult.MatchedCount == updateResult.ModifiedCount)
                {
                    Console.WriteLine($"{DateTime.Now} ***Pokeballs have been reset for all players!***");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} ***Not all pokeball restock updates were successful.***");
                    Console.WriteLine($"Matched Count: {updateResult.MatchedCount}");
                    Console.WriteLine($"Modified Count: {updateResult.ModifiedCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} ***An error occurred during the update operation: {ex.Message}***");
            }
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
                //.Where(p => p.WeeklyExperience > 0)
                //TODO: Uncommented the line above, and remove the line below
                .Where(p => p.WeeklyCaughtPokemon.Sum(pokemon => pokemon.BaseExperience ?? 0) > 0)
                .OrderByDescending(p => p.WeeklyCaughtPokemon.Sum(pokemon => pokemon.BaseExperience ?? 0))
                .ToList();
        }
    }
}

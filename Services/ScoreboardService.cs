using Newtonsoft.Json;
using PokeCord.Data;
using System.Collections.Concurrent;

namespace PokeCord.Services
{
    public class ScoreboardService
    {
        public const int pokeballMax = 50; // Maximum catches per restock (currently hourly)

        //TODO: Monthly scoreboard reset

        // Individual scoreboard data structure
        private static ConcurrentDictionary<ulong, PlayerData> _scoreboard = new ConcurrentDictionary<ulong, PlayerData>();
        // Team scoreboard data structure
        private static List<Team> _teamScoreboard = new List<Team>();
        private readonly string _scoreboardFilePath = "scoreboard.json";
        private readonly string _teamScoreboardFilePath = "teamscoreboard.json";

        public ScoreboardService()
        {
        }
        public bool TryAddPlayerData(ulong userId, PlayerData playerData)
        {
            return _scoreboard.TryAdd(userId, playerData);
        }
        public bool TryGetPlayerData(ulong userId, out PlayerData playerData)
        {
            return _scoreboard.TryGetValue(userId, out playerData);
        }

        public bool TryUpdatePlayerData(ulong userId, PlayerData playerData, PlayerData originalPlayerData)
        {
            return _scoreboard.TryUpdate(userId, playerData, originalPlayerData);
        }
        public async Task ResetPokeballs(object state)
        {
            // Create a temporary copy of scoreboard to avoid conflicts
            var playerDataList = _scoreboard.Values.ToList();

            // Reset Pokeballs for each player in the copy
            foreach (var playerData in playerDataList)
            {
                if (playerData.Pokeballs < pokeballMax)
                {
                    playerData.Pokeballs = pokeballMax;
                }
            }

            // Update the actual scoreboard atomically
            await Task.Run(() => _scoreboard = new ConcurrentDictionary<ulong, PlayerData>(playerDataList.ToDictionary(p => p.UserId, p => p)));

            Console.WriteLine("Pokeballs have been reset for all players!");

            // Save the updated scoreboard
            await SaveScoreboardAsync();
        }

        public List<PlayerData> GetLeaderboard()
        {
            return _scoreboard.Values.ToList().OrderByDescending(p => p.Experience).ToList();
        }

        public List<Team> GetTeams()
        {
            List<Team> teams = _teamScoreboard;
            return teams;
        }

        private async Task LoadTeamScoreboard()
        {
            string filePath = "teamscoreboard.json";
            List<Team> loadedTeamScoreboard = new List<Team>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Team scoreboard data file not found. Creating a new one.");
                SaveTeamScoreboardAsync();
                _teamScoreboard = new List<Team>();
                return;
            }
            else
            {
                Console.WriteLine($"Team scoreboard loaded from {filePath}");
            }

            try
            {
                string jsonData = File.ReadAllText(filePath);
                loadedTeamScoreboard = JsonConvert.DeserializeObject<List<Team>>(jsonData);

                // Handle Team.cs version mismatch
                // Version 1 => 2
                /*
                foreach (var team in teamScoreboard)
                {
                }
                */
                _teamScoreboard = loadedTeamScoreboard;
                return;
            }
            catch (Exception ex)
            {
                // Handle deserialization errors or file access exceptions
                Console.WriteLine("Error loading team scoreboard data: {0}", ex.Message);
                Console.WriteLine("A blank team scoreboard has been loaded.");
                _teamScoreboard = new List<Team>();
                return;
            }
        }

        public async Task SaveTeamScoreboardAsync()
        {
            string filePath = _scoreboardFilePath;

            try
            {
                // Serialize the team scoreboard to JSON string
                string jsonData = JsonConvert.SerializeObject(_teamScoreboard);

                // Write the JSON string to the file asynchronously
                await File.WriteAllTextAsync(filePath, jsonData);
                Console.WriteLine("Team scoreboard data saved successfully.");
            }
            catch (Exception ex)
            {
                // Handle serialization errors or file access exceptions
                Console.WriteLine("Error saving team scoreboard data: {0}", ex.Message);
            }
        }

        public async Task LoadScoreboardAsync()
        {
            if (!File.Exists(_scoreboardFilePath))
            {
                Console.WriteLine("Scoreboard data file not found. Creating a new one.");
                await SaveScoreboardAsync();
                return; // Don't attempt to load from a non-existent file
            }

            try
            {
                string jsonData = await File.ReadAllTextAsync(_scoreboardFilePath);
                _scoreboard = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, PlayerData>>(jsonData);

                // Handle playerData version mismatch
                foreach (var playerData in _scoreboard.Values)
                {
                    // Version 1 => 2
                    if (playerData.Version == 1)
                    {
                        // Upgrade to Version 2
                        playerData.Version = 2;
                        playerData.EarnedBadges = new List<Badge>();
                        //playerData.Badges = new Dictionary<Badge, DateTime>();
                        Console.WriteLine($"{playerData.UserName} upgraded playerData version from 1 to 2");
                    }
                    // Version 2 => 3
                    if (playerData.Version == 2)
                    {
                        playerData.Version = 3;
                        playerData.PokemonDollars = 100; // Give 100 free pokemon dollars to everyone upon upgrade
                    }
                }

                Console.WriteLine($"Scoreboard loaded from {_scoreboardFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading scoreboard data: {0}", ex.Message);
                _scoreboard = new ConcurrentDictionary<ulong, PlayerData>(); // Fallback to empty scoreboard on error
            }
        }

        public async Task SaveScoreboardAsync()
        {
            string jsonData = JsonConvert.SerializeObject(_scoreboard);
            try
            {
                await File.WriteAllTextAsync(_scoreboardFilePath, jsonData);
                Console.WriteLine("Scoreboard data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving scoreboard data: {0}", ex.Message);
            }
        }
    }
}

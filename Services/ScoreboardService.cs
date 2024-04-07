using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using PokeCord.Data;
using PokeCord.Helpers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Linq;
using System.Runtime.CompilerServices;
using PokeApiNet;

namespace PokeCord.Services
{
    public class ScoreboardService
    {
        public const int pokeballMax = 50; // Maximum catches per restock (currently daily)
        public const int weeklyReward = 1000; // Amount of PokemonDollars to award each player of the winning team

        private const ulong felicityPokeCordChannel = 1224090596801511494;
        private const ulong testingPokeCordChannel = 1223317230431895673;

        // Individual scoreboard data structure
        private static ConcurrentDictionary<ulong, PlayerData> _scoreboard = new ConcurrentDictionary<ulong, PlayerData>();
        // Team scoreboard data structure
        private static List<Team> _teamScoreboard = new List<Team>();
        private static Team _winningTeam;
        private readonly string _scoreboardFilePath = "scoreboard.json";
        private readonly string _teamScoreboardFilePath = "teamscoreboard.json";

        public ScoreboardService()
        {
        }
        public bool TryAddPlayerData(ulong userId, PlayerData playerData)
        {
            return _scoreboard.TryAdd(userId, playerData);
        }
        public async Task TryAddPlayerToTeamAsync(ulong userId, int teamId)
        {
            // Find the team with the given teamId
            Team team = _teamScoreboard.FirstOrDefault(t => t.Id == teamId);
            // Add the reference to the player to the team
            team.Players.Add(userId);
            // Save the updated team scoreboard
            await SaveTeamScoreboardAsync();
            // Update the player's TeamId in the individual scoreboard
            if (TryGetPlayerData(userId, out var playerData))
            {
                playerData.TeamId = teamId;
                await SaveScoreboardAsync();
            }
        }
        public bool TryGetPlayerData(ulong userId, out PlayerData playerData)
        {

            return _scoreboard.TryGetValue(userId, out playerData);
        }

        public bool TryUpdatePlayerData(ulong userId, PlayerData playerData, PlayerData originalPlayerData)
        {
            return _scoreboard.TryUpdate(userId, playerData, originalPlayerData);
        }
        public async Task ResetPokeballsAsync(object state)
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

        public async Task StartWeeklyTeamsEventAsync(DiscordSocketClient client)
        {

            string message = $"";

            // Make announcement
            var channel = await client.GetChannelAsync(testingPokeCordChannel) as IMessageChannel;

            if (channel == null)
            {
                Console.WriteLine("Channel not found!");
                return;
            }
            await channel.SendMessageAsync(message);
        }


        public async Task EndWeeklyTeamsEventAsync(DiscordSocketClient client)
        {
            Console.WriteLine("*** The weekly event has ended *** Time: " + DateTime.UtcNow.ToString());
            // Find winning team
            List<Team> teams = GetTeams();
            _winningTeam = teams.First();
            string reward = weeklyReward.ToString("N0");
            string message = $"Attention Trainers! The weekly Team Championship has ended! The results are...\n";
            for (int i = 0; i < teams.Count; i++)
            {
                if (teams.Count <= 0)
                {
                    message += $"There were no teams created during this event, or something went horribly wrong.";
                    break;
                }
                string teamExp = teams[i].TeamExperience.ToString("N0");
                // Get list of name of each member of team
                List<String> members = new List<string>();
                foreach (ulong player in teams[i].Players)
                {
                    if (_scoreboard.TryGetValue(player, out var playerData))
                    {
                        members.Add(playerData.UserName);
                    }
                }
                string membersList = string.Join(", ", members);
                message += $"{i + 1}. Team {teams[i].Name}: {teamExp} exp.\n" +
                          $"Trainers: {membersList}\n";
            }
            message += $"\nEach member of the winning team is awarded {reward} Pokémon Dollars!";
            // Dish out rewards
            foreach (ulong player in _winningTeam.Players)
            {
                if (_scoreboard.TryGetValue(player, out var playerData))
                {
                    playerData.PokemonDollars += weeklyReward;
                }
                else
                {
                    Console.WriteLine($"Could not give award to user: {player}");
                }
            }
            // Make announcement
            var channel = await client.GetChannelAsync(testingPokeCordChannel) as IMessageChannel;

            if (channel == null)
            {
                Console.WriteLine("Channel not found!");
                return;
            }
            await channel.SendMessageAsync(message);
            await ResetTeamsAsync(null);
        }

        public async Task ResetTeamsAsync(object state)
        {
            // Create a temporary copy of scoreboard to avoid conflicts
            var playerDataList = _scoreboard.Values.ToList();

            foreach (PlayerData playerData in playerDataList)
            {
                if (playerData.TeamId != -1)
                {
                    playerData.TeamId = -1;
                    playerData.WeeklyExperience = 0;
                    playerData.WeeklyCaughtPokemon = new List<PokemonData>();
                }
            }

            // Set empty team scoreboard
            _teamScoreboard = new List<Team>();

            // Save both scoreboards
            await SaveScoreboardAsync();
            await SaveTeamScoreboardAsync();

            Console.WriteLine("Teams have been reset at UTC: " + DateTime.UtcNow.ToString());
        }

        public List<PlayerData> GetLeaderboard()
        {
            return _scoreboard.Values.ToList().OrderByDescending(p => p.WeeklyExperience).ToList();
        }

        public List<Team> GetTeams()
        {
            List<Team> teams = _teamScoreboard;
            foreach (Team team in teams)
            {
                // Update team exp
                int teamExperience = 0;
                foreach (ulong playerId in team.Players)
                {
                    if (_scoreboard.TryGetValue(playerId, out var playerData))
                    {
                        teamExperience += playerData.Experience;
                    }
                }
                team.TeamExperience = teamExperience;
            }
            teams = teams.OrderByDescending(t => t.TeamExperience).ToList();
            return teams;
        }
        public async void AddTeam(Team team)
        {
            _teamScoreboard.Add(team);
            await SaveScoreboardAsync();
        }

        public async Task LoadTeamScoreboardAsync()
        {
            string filePath = "teamscoreboard.json";
            List<Team> loadedTeamScoreboard = new List<Team>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Team scoreboard data file not found. Creating a new one.");
                await SaveTeamScoreboardAsync();
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
            string filePath = _teamScoreboardFilePath;

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
                        playerData.WeeklyExperience = 0;
                        playerData.TeamId = -1;
                        playerData.WeeklyCaughtPokemon = new List<PokemonData>();
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

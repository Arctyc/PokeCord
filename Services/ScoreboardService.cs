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
using PokeCord.SlashCommands;

namespace PokeCord.Services
{
    public class ScoreboardService
    {
        public const int pokeballRestockAmount = 50; // Amount of Pokeballs given per restock (currently daily)
        public const int currencyCap = 5000; // Max amount of pokemon dollars a player can have

        private const ulong felicityPokeCordChannel = 1224090596801511494;
        //private const ulong testingPokeCordChannel = 1223317230431895673;
        private const ulong pokecordChannel = felicityPokeCordChannel; // FIX: use felicity for release

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
            Console.WriteLine($"Scoreboard is trying to get PlayerData ID: {userId}");
            return _scoreboard.TryGetValue(userId, out playerData);
        }

        public bool TryUpdatePlayerData(ulong userId, PlayerData playerData, PlayerData originalPlayerData)
        {
            return _scoreboard.TryUpdate(userId, playerData, originalPlayerData);
        }
        public async Task RestockPokeballsAsync(object state)
        {
            // Create a temporary copy of scoreboard to avoid conflicts
            var playerDataList = _scoreboard.Values.ToList();

            // Reset Pokeballs for each player in the copy
            foreach (var playerData in playerDataList)
            {
                if (playerData.Pokeballs < pokeballRestockAmount)
                {
                    playerData.Pokeballs = pokeballRestockAmount;
                }
            }

            // Update the actual scoreboard atomically
            await Task.Run(() => _scoreboard = new ConcurrentDictionary<ulong, PlayerData>(playerDataList.ToDictionary(p => p.UserId, p => p)));

            Console.WriteLine("Pokeballs have been reset for all players!");

            // Save the updated scoreboard
            await SaveScoreboardAsync();
        }

        // TODO: Move Weekly Teams Competition event methods to a unique class
        public async Task StartWeeklyTeamsEventAsync(DiscordSocketClient client)
        {
            Console.WriteLine("*** The weekly event has started *** Time: " + DateTime.UtcNow.ToString());

            // Call method to reset teams
            await ResetTeamsAsync(null);

            // Get channel to post to
            var channel = await client.GetChannelAsync(pokecordChannel) as IMessageChannel;
            if (channel == null)
            {
                Console.WriteLine("Channel not found!");
                return;
            }

            // Set message
            string message = $"\nAttention Trainers! The weekly Team Championship has started! You may now create and join new teams!\n" +
                             $"The winners will be announced (and prizes handed out) on Sunday at 12:00 AM UTC";

            // Make announcement            
            await channel.SendMessageAsync(message);
        }

        public async Task EndWeeklyTeamsEventAsync(DiscordSocketClient client)
        {
            Console.WriteLine("*** The weekly event has ended *** Time: " + DateTime.UtcNow.ToString());

            // Get channel to post to
            var channel = await client.GetChannelAsync(pokecordChannel) as IMessageChannel;
            if (channel == null)
            {
                Console.WriteLine("Channel not found! *** Bailed out of EndWeeklyTeamsEventAsync() method!!! ***");
                return;
            }

            // Find the top 3 teams (or all teams if fewer than 3)
            List<Team> teams = GetTeams();
            int numTeams = teams.Count;
            int totalPlayers = teams.SelectMany(t => t.Players).Distinct().Count();
            int totalReward = totalPlayers * 500;

            string message = GetEndWeeklyCompetitionHeader(numTeams);

            // Distribute the rewards to the top teams
            if (numTeams > 0)
            {
                message += await DistributeRewardsToTeams(teams, totalReward);
            }

            // Update the scoreboard
            await SaveScoreboardAsync();

            // Make announcement
            await channel.SendMessageAsync(message);
        }

        private string GetEndWeeklyCompetitionHeader(int numTeams)
        {
            return numTeams == 0
                ? "\nAttention Trainers! The weekly Team Championship has ended!\n" +
                  "There were no teams created during this event, or something went horribly wrong."
                : "\nAttention Trainers! The weekly Team Championship has ended! The results are...\n";
        }

        private async Task<string> DistributeRewardsToTeams(List<Team> teams, int totalReward)
        {
            string message = string.Empty;

            for (int i = 0; i < Math.Min(teams.Count, 3); i++)
            {
                Team team = teams[i];
                int teamReward = GetTeamReward(i, teams.Count, totalReward);
                message += GetTeamResultMessage(i + 1, team, teamReward);

                await UpdatePlayerScores(team, teamReward);
            }

            return message;
        }

        private int GetTeamReward(int teamRank, int numTeams, int totalReward)
        {
            if (numTeams == 1)
            {
                return (int)totalReward;
            }
            if (numTeams == 2)
            {
                switch (teamRank)
                {
                    case 0:
                        return (int)(totalReward * 0.7); // 70% if only 2 teams compete
                    case 1:
                        return (int)(totalReward * 0.3); // 30% if only 2 teams compete
                }
            }

            switch (teamRank)
            {
                case 0:
                    return (int)(totalReward * 0.5); // 50% of the total reward

                case 1:
                    return (int)((totalReward * 0.3) / (numTeams - 1)); // Split the remaining 50% equally among the other teams
                case 2:
                    return (int)((totalReward * 0.2) / (numTeams - 1)); // Split the remaining 50% equally among the other teams
                default:
                    Console.WriteLine($"Attempted to get reward for a {teamRank + 1}th place team.");
                    return 0;
            }
        }

        private string GetTeamResultMessage(int rank, Team team, int teamReward)
        {
            string teamExp = team.TeamExperience.ToString("N0");
            List<string> teamMemberNames = new List<string>();

            foreach (ulong playerId in team.Players)
            {
                if (_scoreboard.TryGetValue(playerId, out var playerData))
                {
                    teamMemberNames.Add(playerData.UserName);
                }
            }

            string membersList = string.Join(", ", teamMemberNames);
            return $"{rank}. Team {team.Name}: {teamExp} exp.\n" +
                   $"Trainers: {membersList}\n" +
                   $"Each member of this team is awarded {(teamReward / team.Players.Count).ToString("N0")} Pokémon Dollars!\n\n";
        }

        private async Task UpdatePlayerScores(Team team, int teamReward)
        {
            int maxRetries = 3;
            int retryDelay = 500; // 0.5 seconds

            // Loop through each player on the team
            foreach (ulong playerId in team.Players)
            {
                int retryCount = 0;
                bool updateSuccessful = false;

                while (retryCount < maxRetries && !updateSuccessful)
                {
                    try
                    {
                        if (_scoreboard.TryGetValue(playerId, out var originalPlayerData))
                        {
                            // Add pokemondollars in the amount of the team reward divided evenly amongst the team members
                            PlayerData playerData = originalPlayerData;
                            playerData.PokemonDollars += teamReward / team.Players.Count;
                            if (playerData.PokemonDollars > currencyCap) { playerData.PokemonDollars = currencyCap; }
                            updateSuccessful = await SavePlayerDataAsync(playerData, originalPlayerData);
                            if (updateSuccessful) // Log for tracking
                            {
                                Console.WriteLine($"Prize of {teamReward / team.Players.Count} successfully awarded to {playerData.UserName}.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating player score for user {playerId}: {ex.Message}");
                        retryCount++;
                        await Task.Delay(retryDelay);
                    }
                }
                if (!updateSuccessful)
                {
                    Console.WriteLine($"Failed to update player score for user {playerId} after {maxRetries} retries.");
                }
            }
        }

        public async Task ResetTeamsAsync(object state)
        {
            Console.WriteLine("ResetTeamsAsync() Called here!");
            await LoadScoreboardAsync();
            // Reset all weekly values for entire scoreboard
            foreach (var playerData in _scoreboard.Values)
            {
                if (playerData.TeamId != -1)
                {
                    
                    playerData.TeamId = -1;
                }
                playerData.WeeklyExperience = 0;
                playerData.WeeklyCaughtPokemon = new List<PokemonData>();
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

        public async void AddTeam(Team team)
        {
            _teamScoreboard.Add(team);
            await SaveScoreboardAsync();
        }

        public List<Team> GetTeams()
        {
            List<Team> teams = _teamScoreboard;
            if (teams.Count == 0)
            {
                return new List<Team>();
            }
            foreach (Team team in teams)
            {
                // Update team exp
                int teamExperience = 0;
                foreach (ulong playerId in team.Players)
                {
                    if (_scoreboard.TryGetValue(playerId, out var playerData))
                    {
                        teamExperience += playerData.WeeklyExperience;
                    }
                }
                team.TeamExperience = teamExperience;
            }
            // Order list by team experience before returning
            teams = teams.OrderByDescending(t => t.TeamExperience).ToList();
            return teams;
        }

        public async Task<bool> SavePlayerDataAsync(PlayerData playerData, PlayerData originalPlayerData)
        {
            // Update the player's data in the scoreboard
            if (_scoreboard.TryUpdate(playerData.UserId, playerData, originalPlayerData))
            {
                // Save the updated scoreboard
                await SaveScoreboardAsync();
                return true;
            }
            else
            {
                Console.WriteLine($"Unable to save playerdata for {playerData.UserName}");
                return false;
            }
        }

        public async Task LoadTeamScoreboardAsync()
        {
            string filePath = "teamscoreboard.json";
            List<Team> loadedTeamScoreboard = new List<Team>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Team scoreboard data file not found. Creating a new one.");
                _teamScoreboard = new List<Team>();
                //await ResetTeamsAsync(null); // TESTING: Only use to reset teams by deleting the teamScoreboard.json file
                await SaveTeamScoreboardAsync();
                
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
                int countUpdated = 0;
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
                        countUpdated++;
                    }
                    // Version 2 => 3
                    if (playerData.Version == 2)
                    {
                        playerData.Version = 3;
                        playerData.PokemonDollars = 100; // Give 100 free pokemon dollars to everyone upon upgrade
                        playerData.WeeklyExperience = 0;
                        playerData.TeamId = -1;
                        playerData.WeeklyCaughtPokemon = new List<PokemonData>();
                        countUpdated++;
                    }
                    // Version 3 => 4
                    if (playerData.Version == 3)
                    {
                        playerData.Version = 4;
                        playerData.PokeMartItems = new Dictionary<string, int>();
                        countUpdated++;
                    }
                }
                Console.WriteLine($"Scoreboard loaded from {_scoreboardFilePath}");
                if (countUpdated > 0)
                {
                    await SaveScoreboardAsync();
                    Console.WriteLine($"{countUpdated} players updated version");
                }
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

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PokeCord.Data;
using PokeCord.Helpers;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace PokeCord.Services
{
    public class TeamChampionshipService
    {
        private readonly PlayerDataService _playerDataService;

        private const ulong felicityPokeCordChannel = 1224090596801511494;
        //private const ulong testingPokeCordChannel = 1223317230431895673;
        private const ulong pokecordChannel = felicityPokeCordChannel;

        public const int currencyCap = 5000; // Max amount of pokemon dollars a player can have

        private readonly IMongoCollection<Team> _teamChampionshipCollection;
        public TeamChampionshipService(MongoDBClientProvider mongoDBClient, IServiceProvider services)
        {
            _playerDataService = services.GetRequiredService<PlayerDataService>();

            var database = mongoDBClient.GetDatabase();
            _teamChampionshipCollection = database.GetCollection<Team>("TeamChampionship");
        }

        public async Task<bool> TryAddTeamAsync(Team team)
        {
            var existingTeam = await _teamChampionshipCollection.Find(t => t.Name == team.Name).FirstOrDefaultAsync();
            if (existingTeam == null)
            {
                await _teamChampionshipCollection.InsertOneAsync(team);
                return true;
            }
            return false;
        }

        public async Task<bool> TryAddPlayerToTeamAsync(ulong userId, Team team)
        {
            // Add player to list
            team.Players.Add(userId);
            // Update team object
            var result = await _teamChampionshipCollection.ReplaceOneAsync(t => t.Id == team.Id, team);
            return result.ModifiedCount > 0;
        }

        public async Task<List<Team>> GetTeamsAsync()
        {
            return await _teamChampionshipCollection.Find(_ => true).SortByDescending(t => t.TeamExperience).ToListAsync();
        }

        public async Task ResetTeamsAsync(object? state)
        {
            // Clear all records from TeamChampionship collection
            await _teamChampionshipCollection.DeleteManyAsync(Builders<Team>.Filter.Empty);

            // Update all players setting TeamId to -1
            var updateDefinition = Builders<PlayerData>.Update.Set(p => p.TeamId, -1);
            await _playerDataService.TryUpdatePlayersAsync(updateDefinition);

            Console.WriteLine("Teams have been reset at UTC: " + DateTime.UtcNow.ToString());
        }

        public async Task StartWeeklyTeamsEventAsync(DiscordSocketClient client)
        {
            Console.WriteLine("\n*** The weekly event has started *** Time: " + DateTime.UtcNow.ToString());

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
            List<Team> teams = await GetTeamsAsync();
            int numTeams = teams.Count;
            int totalPlayers = teams.SelectMany(t => t.Players).Distinct().Count();
            int totalReward = totalPlayers * 500;

            string message = GetEndWeeklyCompetitionHeader(numTeams);

            // Distribute the rewards to the top teams
            if (numTeams > 0)
            {
                message += await DistributeRewardsToTeams(teams, totalReward);
            }

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
                message += GetTeamResultMessageAsync(i + 1, team, teamReward);

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
                    return (int)(totalReward * 0.5); // 50% to Team 1
                case 1:
                    return (int)(totalReward * 0.3); // 30% to Team 2
                case 2:
                    return (int)(totalReward * 0.2); // 20% to Team 3
                default:
                    Console.WriteLine($"Attempted to get reward for a {teamRank + 1}th place team.");
                    return 0;
            }
        }

        private async Task<string> GetTeamResultMessageAsync(int rank, Team team, int teamReward)
        {
            string teamExp = team.TeamExperience.ToString("N0");
            List<string> teamMemberNames = new List<string>();

            // Find username of team's members
            foreach (ulong playerId in team.Players)
            {
                // Add each name to a list
                var playerData = await _playerDataService.TryGetPlayerDataAsync(playerId);
                if (playerData != null)
                {
                    teamMemberNames.Add(playerData.UserName);
                }
                else
                {
                    Console.WriteLine($"Could not get username for {playerId} on Team {team.Name}");
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
                        PlayerData? playerData = await _playerDataService.TryGetPlayerDataAsync(playerId);
                        if (playerData != null)
                        {
                            // Add pokemondollars in the amount of the team reward divided evenly amongst the team members
                            playerData.PokemonDollars += teamReward / team.Players.Count;
                            if (playerData.PokemonDollars > currencyCap) { playerData.PokemonDollars = currencyCap; } // Do not exceed currency cap
                            updateSuccessful = await _playerDataService.TryUpdatePlayerDataAsync(playerData.UserId, playerData);
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
    }
}

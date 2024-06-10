﻿using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Linq;

namespace PokeCord.SlashCommands
{
    public class TeamJoinModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlayerDataService _playerDataService;
        private readonly TeamChampionshipService _teamChampionshipService;
        public const int teamJoinCost = TeamCreateModule.teamCreateCost; // Set team join cost to equal team create cost
        public const int teamCap = 4; // Maximum number of players per team

        public TeamJoinModule(IServiceProvider services)
        {
            Console.Write("Loaded command: teamjoin\n");
            _playerDataService = services.GetRequiredService<PlayerDataService>();
            _teamChampionshipService = services.GetRequiredService<TeamChampionshipService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("teamjoin", "Join a Poké Team.")]
        public async Task JoinTeamCommand(
            [Summary("team", "The Poké Team you would like to join.")]
            [Autocomplete(typeof(TeamAutocompleter))] string teamToJoin)
        {
            ulong userId = Context.User.Id;
            string username = Context.User.GlobalName;

            // Refuse if Sunday i.e. After weekly end, before weekly start
            if (DateTime.Now.DayOfWeek.ToString() == "Sunday")
            {
                await RespondAsync("The next weekly Team Championship will open at 12:00 AM Monday UTC.");
                return;
            }

            // Get player data
            PlayerData playerData = await _playerDataService.TryGetPlayerDataAsync(userId);
            if (playerData == null)
            {
                // PlayerData does not exist for this userId
                Console.WriteLine($"PlayerData found for {username} {userId}");
                await RespondAsync($"No data for {username} found. Have you caught your first Pokémon?");
                return;
            }

            // Get all teams
            List<Team> allTeams = await _teamChampionshipService.GetTeamsAsync();

            // Make sure teamToJoin is one of the teams
            if (!allTeams.Any(t => t.Name == teamToJoin))
            {
                Console.WriteLine($"{username} tried to join Team {teamToJoin} but it does not exist.");
                await RespondAsync($"You cannot join Team {teamToJoin} because it does not exist. Did you mean to use /teamcreate?");
                return;
            }

            // Get team data
            Team team = allTeams.FirstOrDefault(t => t.Name == teamToJoin);
            Console.WriteLine($"{username} is triyng to join Team {team.Name}. Team ID: {team.Id}. User is on team: {playerData.TeamId}");
            if (team == null)
            {
                Console.WriteLine($"Could not find team data for {teamToJoin}");
                await RespondAsync($"Could not find team data for {teamToJoin}. Arctyc screwed something up.");
                return;
            }

            // Check player is not already on a team
            if (playerData.TeamId != -1)
            {
                Team? existingTeam = allTeams.FirstOrDefault(t => t.Id == playerData.TeamId);
                if (existingTeam != null)
                {
                    await RespondAsync($"You are already on Team {existingTeam.Name}!");
                }
                else
                {
                    // The player's TeamId is not -1, but the team no longer exists in the scoreboard
                    playerData.TeamId = -1;
                    await _playerDataService.TryUpdatePlayerDataAsync(userId, playerData);
                    await RespondAsync($"PlayerData team ID error. Try again, if you see this message twice, contact @ArctycFox");
                }
                return;
            }

            // Check that player can afford to join a team
            if (playerData.PokemonDollars < teamJoinCost)
            {
                int remaining = teamJoinCost - playerData.PokemonDollars;
                await RespondAsync($"You need {remaining.ToString()} more Pokémon Dollars to join a team.");
                return;
            }

            // Check that there is room on the team for the player
            if (team.Players.Count >= teamCap)
            {
                await RespondAsync($"Sorry, Team {team.Name} is full. Why not make your own team and take them down!?");
                return;
            }

            // Join Team
            bool success = await _teamChampionshipService.TryAddPlayerToTeamAsync(userId, team);
            if (success)
            {
                // Adjust playerData
                playerData.PokemonDollars -= teamJoinCost;
                playerData.TeamId = team.Id;

                // Save data
                await _playerDataService.TryUpdatePlayerDataAsync(userId, playerData);

                // Reply in Discord
                await RespondAsync($"{username} is now a member of Team {team.Name}");
            }
            else
            {
                // Unable to add player to team
                Console.WriteLine($"Unable to add {username} to team {teamToJoin}.");
                await RespondAsync($"There was an error joining Team {teamToJoin}.");
                return;
            }            
        }
    }
}

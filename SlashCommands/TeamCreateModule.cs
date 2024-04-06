using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.SlashCommands
{
    public class TeamCreateModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ScoreboardService scoreboardService;
        public const int teamCreateCost = 1000; // Cost in poke dollars to create a team

        public TeamCreateModule(IServiceProvider services)
        {
            Console.Write("Loaded command: teamcreate\n");
            scoreboardService = services.GetRequiredService<ScoreboardService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("teamcreate", "Create a Poké Team. Fill in the blank: Team ___.")]
        public async Task CreateTeamCommand([Choice("team", "Team:")] string teamName)
        {
            ulong userId = Context.User.Id;
            string username = Context.User.GlobalName;

            // Get player data
            PlayerData playerData = new PlayerData();
            if (scoreboardService.TryGetPlayerData(userId, out playerData))
            {
            }
            else
            {
                // PlayerData does not exist for this userId
                await RespondAsync($"No data for {username} found. Have you caught your first Pokémon?");
            }

            // Check if user can afford to create team
            if (playerData.PokemonDollars < teamCreateCost)
            {
                int remaining = teamCreateCost - playerData.PokemonDollars;
                await RespondAsync($"You need {remaining.ToString()} more Pokémon Dollars to create a team.");
            }

            // Check if team exists
            List<Team> existingTeams = scoreboardService.GetTeams();
            bool exists = false;
            foreach (Team t in existingTeams)
            {
                if (teamName == t.Name)
                {
                    exists = true;
                }
            }

            // Build team
            Team team = new Team();
            team.Id = existingTeams.Count + 1;
            team.Name = teamName;
            team.Players.Add(playerData);
            team.TeamExperience = playerData.WeeklyExperience;
            // Add team
            scoreboardService.AddTeam(team); // Saved to file in service

            await RespondAsync($"Congratulations, {username}! You are the new leader of Team {teamName}");

            /*
            // Get the PlayerData instance from the scoreboard
            PlayerData playerData = new PlayerData();
            if (scoreboardService.TryGetPlayerData(userId, out playerData))
            {
            }
            else
            {
                // PlayerData does not exist for this userId
                await RespondAsync($"No data for {username} found. Have you caught your first Pokémon?");
            }

            // Pass to TeamManager
            (string message, Team team) = TeamManager.CreateTeam(Context, playerData, teamCreateCost);
            if (team.Id == -1)
            {
                // Do not charge
                await RespondAsync(message);
            }
            teamScoreboard.Add(team);
            await SaveTeamScoreboardAsync();
            playerData.PokemonDollars -= teamCreateCost;

            // Reply in Discord
            await RespondAsync(createMessage);

            ////

            string message;
            List<Team> teams = scoreboardService.GetTeams();
            if (teams == null)
            {
                teams = new List<Team>();
            }
            Team team = new Team();
            team.Id = -1; // If returned with this ID, currency will not be deducted from user
            string? teamName = Context.Options.First().Value.ToString();

            // Verify user can afford team creation cost
            if (playerData.PokemonDollars < teamCreateCost)
            {
                message = $"Sorry, you need {teamCreateCost} Pokémon Dollars to create a team. " +
                          $"You current have {playerData.PokemonDollars}";
            }
            // Check for existing name
            bool exists = false;
            foreach (Team existingTeam in teams)
            {
                if (teamName == existingTeam.Name)
                {
                    exists = true;
                }
            }
            if (exists)
            {
                message = $"There is already a team named Team {teamName}. Please choose something different.";
                return (message, team);
            }

            // Build team
            team.Id = teams.Count + 1;
            team.Name = teamName;
            team.TeamExperience = playerData.Experience;
            team.Players.Add(playerData);

            message = $"{Context.User.GlobalName} has created Team {teamName}! If you'd like to join this team, type /teamjoin {teamName}.";
            return (message, team);
            */
        }
    }
}

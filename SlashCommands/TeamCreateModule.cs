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
        //public const int teamCreateCost = 1000; // Cost in poke dollars to create a team
        public const int teamCreateCost = 0;

        public TeamCreateModule(IServiceProvider services)
        {
            Console.Write("Loaded command: teamcreate\n");
            scoreboardService = services.GetRequiredService<ScoreboardService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("teamcreate", "Create a Poké Team. Fill in the blank: Team ___.")]
        public async Task CreateTeamCommand([Summary("Team")] string newTeamName)
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
            
            // Check player is not already on a team
            if (playerData.TeamId != -1)
            {
                string? teamName = scoreboardService.GetTeams().Where(x => x.Id == playerData.TeamId).Select(x => x.Name).FirstOrDefault();
                await RespondAsync($"You are already on Team {teamName}");
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
                if (newTeamName == t.Name)
                {
                    exists = true;
                }
            }
            if (exists)
            {
                await RespondAsync($"Team {newTeamName} already exists! Did you mean to use /teamjoin?");
            }

            // Build team
            Team team = new Team();
            team.Id = existingTeams.Count + 1;
            team.Name = newTeamName;
            team.Players.Add(playerData);
            team.TeamExperience = playerData.WeeklyExperience;

            // Add team to list in memory
            scoreboardService.AddTeam(team); // Saved to file in service

            // Adjust playerData
            playerData.PokemonDollars -= teamCreateCost;
            playerData.TeamId = team.Id;

            // Save data
            await scoreboardService.SaveTeamScoreboardAsync();
            await scoreboardService.SaveScoreboardAsync();

            // Respond in Discord
            await RespondAsync($"Congratulations, {username}! You are the new leader of Team {newTeamName}");
        }
    }
}

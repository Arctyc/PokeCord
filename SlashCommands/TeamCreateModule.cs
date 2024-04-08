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
        public const int teamCreateCost = 500; // Cost in poke dollars to create a team

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

            // Refuse if after WeeklyTimerEnd or before WeeklyTimerStart
            if (DateTime.Now.DayOfWeek.ToString() == "Sunday")
            {
                await RespondAsync("The next weekly Team Championship will open on Monday at 12:00 AM UTC.");
                return;
            }

            // Get player data
            PlayerData playerData = new PlayerData();
            if (scoreboardService.TryGetPlayerData(userId, out playerData))
            {
            }
            else
            {
                // PlayerData does not exist for this userId
                Console.WriteLine($"PlayerData found for {username} {userId}");
                await RespondAsync($"No data for {username} found. Have you caught your first Pokémon?");
                return;
            }

            // Check player is not already on a team
            if (playerData.TeamId != -1)
            {
                Team? existingTeam = scoreboardService.GetTeams().FirstOrDefault(t => t.Id == playerData.TeamId);
                if (existingTeam != null)
                {
                    await RespondAsync($"You are already on Team {existingTeam.Name}");
                }
                else
                {
                    // The player's TeamId is not -1, but the team no longer exists in the scoreboard
                    playerData.TeamId = -1;
                    await scoreboardService.SaveScoreboardAsync();
                    await RespondAsync($"PlayerData team ID error. Try again, if you see this message twice, contact @ArctycFox");
                }
                return;
            }

            // Check if user can afford to create team
            if (playerData.PokemonDollars < teamCreateCost)
            {
                int remaining = teamCreateCost - playerData.PokemonDollars;
                await RespondAsync($"You need {remaining.ToString()} more Pokémon Dollars to create a team.");
                return;
            }

            // Check if team exists
            List<Team> existingTeams = scoreboardService.GetTeams();
            if (existingTeams.Any(t => t.Name == newTeamName))
            {
                await RespondAsync($"Team {newTeamName} already exists! Did you mean to use /teamjoin?");
                return;
            }



            // Build team
            Team team = new Team
            {
                Id = existingTeams.Count + 1,
                Name = newTeamName,
                Players = new List<ulong> { userId },
                TeamExperience = playerData.WeeklyExperience
            };

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

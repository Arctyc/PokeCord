﻿using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    public class LeaderboardModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ScoreboardService scoreboardService;

        public LeaderboardModule(IServiceProvider services)
        {
            Console.Write("Loaded command: pokeleaderboard\n");
            scoreboardService = services.GetRequiredService<ScoreboardService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokeleaderboard", "Show a list of the trainers with the most exp.")]
        public async Task LeaderboardCommand()
        {
            TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            // Log time til next pokeball in console - cheeky workaround to check it
            Console.WriteLine("Time until Pokeball reset: " + delay);

            // Get a sorted list of players
            var leaders = scoreboardService.GetLeaderboard();

            // Add a message line for each of the top 10
            List<string> leaderMessages = new List<string>();
            int leaderCount = 10;
            if (leaders.Count < 10)
            {
                leaderCount = leaders.Count;
            }
            for (int i = 0; i < leaderCount; i++)
            {
                int averageExp = 0;
                string leaderName = leaders[i].UserName;
                string leaderExp = leaders[i].WeeklyExperience.ToString("N0");
                if (leaders[i].WeeklyExperience <= 0)
                {
                    averageExp = 0;
                }
                else
                {
                    averageExp = leaders[i].WeeklyExperience / leaders[i].WeeklyCaughtPokemon.Count;
                }
                string message = $"{i + 1}. {leaderName} - {leaderExp} exp. Average exp/catch: {averageExp}";
                leaderMessages.Add(message);
            }
            // Output message to discord
            string leaderboardMessage = string.Join("\n", leaderMessages);
            await RespondAsync($"Weekly top {leaderCount} trainers:\n" + leaderboardMessage);
        }
    }
}

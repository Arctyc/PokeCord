using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    public class LeaderboardModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlayerDataService _playerDataService;
        private readonly TeamChampionshipService _teamChampionshipService;

        public LeaderboardModule(IServiceProvider services)
        {
            Console.Write("Loaded command: pokeleaderboard\n");
            _playerDataService = services.GetRequiredService<PlayerDataService>();
            _teamChampionshipService = services.GetRequiredService<TeamChampionshipService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokeleaderboard", "Show a list of the trainers with the most exp.")]
        public async Task LeaderboardCommand()
        {
            // Console log for checking pokeball restock whenever leaderboard is called
            TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            // Log time til next pokeball restock in console - cheeky workaround to check it
            Console.WriteLine("Time until Pokeball restock: " + delay);

            // Refuse if after WeeklyTimerEnd or before WeeklyTimerStart
            if (DateTime.Now.DayOfWeek.ToString() == "Sunday")
            {
                await RespondAsync("The next weekly Team Championship will open on Monday at 12:00 AM UTC.\n" +
                                   "To see your overall stats, use /pokescore.");
            }

            // Get a sorted list of players
            var leaders = await _playerDataService.GetWeeklyLeaderboardAsync();

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
                int leaderWeeklyExp = leaders[i].WeeklyCaughtPokemon.Sum(p => p.BaseExperience ?? 0);
                string leaderExp = leaderWeeklyExp.ToString("N0");
                if (leaderWeeklyExp <= 0)
                {
                    averageExp = 0;
                }
                else
                {
                    averageExp = leaderWeeklyExp / leaders[i].WeeklyCaughtPokemon.Count;
                }
                string message = $"{i + 1}. {leaderName} - {leaderExp} exp. Avg: {averageExp}";
                leaderMessages.Add(message);
            }
            // Output message to discord
            string leaderboardMessage = string.Join("\n", leaderMessages);
            await RespondAsync($"Weekly Top {leaderCount} Trainers:\n" + leaderboardMessage);
        }
    }
}

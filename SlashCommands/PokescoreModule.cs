using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Linq;

namespace PokeCord.SlashCommands
{
    public class PokescoreModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ScoreboardService scoreboardService;
        private readonly BadgeService badgeService;

        public PokescoreModule(IServiceProvider services)
        {
            Console.Write("Loaded command: pokescore\n");
            scoreboardService = services.GetRequiredService<ScoreboardService>();
            badgeService = services.GetRequiredService<BadgeService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokescore", "View your PokeCord exp, Pokémon Dollars, badge count, and best catch.")]
        public async Task PokescoreCommand()
        {
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            List<Badge> badges = badgeService.GetBadges();
            Console.WriteLine($"{username} used pokescore");

            // Get the PlayerData instance from the scoreboard
            PlayerData playerData = new PlayerData();

            if (scoreboardService.TryGetPlayerData(userId, out playerData))
            {
                Console.WriteLine($"PlayerData found for {username} {userId}");
            }

            if (playerData != null)
            {
                // Get all players
                List<PlayerData> allPlayers = scoreboardService.GetLeaderboard();
                // Get player's rank
                int playerRank = allPlayers.IndexOf(playerData);
                string rank;
                if (playerRank == -1)
                {
                    rank = "Not Found";
                }
                else
                {
                    rank = (playerRank + 1).ToString();
                }

                // Format ints as numerical string
                string lifetimeExperience = playerData.Experience.ToString("N0");
                string weeklyExperience = playerData.WeeklyExperience.ToString("N0");
                string pokemonDollars = playerData.PokemonDollars.ToString("N0");
                // Initialize and set output variables
                string playerTeam = "";
                List<PokemonData> caughtPokemon = playerData.CaughtPokemon;
                int catches = caughtPokemon.Count;
                int weeklyAverageExp = 0;
                int lifetimeAverageExp = 0;
                
                // Get all teams
                List<Team> allTeams = scoreboardService.GetTeams();

                // Build output
                if (playerData.CaughtPokemon.Any())
                {
                    PokemonData bestPokemon = caughtPokemon.OrderByDescending(p => p.BaseExperience).FirstOrDefault();

                    // Set lifetime average exp string
                    lifetimeAverageExp = playerData.Experience / playerData.CaughtPokemon.Count;

                    // Set weekly average exp string
                    if (playerData.WeeklyExperience <= 0 || playerData.WeeklyCaughtPokemon.Count <= 0)
                    {
                        weeklyAverageExp = 0;
                        rank = "∞";
                    }
                    else
                    {
                        weeklyAverageExp = playerData.WeeklyExperience / playerData.WeeklyCaughtPokemon.Count;
                    }
                    bool onTeam = false;
                    if (playerData.TeamId != -1)
                    {
                        onTeam = true;
                        playerTeam = allTeams.FirstOrDefault(t => t.Id == playerData.TeamId).Name;
                    }
                    // Format Discord reply
                    string message = $"{(onTeam ? $"[Team {playerTeam} {username}]" : $"{username}")} has caught {catches} Pokémon totalling {lifetimeExperience} exp. Average exp: {lifetimeAverageExp}\n" +
                                     $"Weekly Rank: {rank}. Lifetime Rank: {playerRank}\n" +
                                     $"Weekly Experience: {weeklyExperience}. Weekly Average Exp: {weeklyAverageExp}\n" +
                                     $"Their best catch was this {(bestPokemon.Shiny ? ":sparkles:SHINY:sparkles: " : "")}" +
                                     $"{CleanOutput.FixPokemonName(bestPokemon.Name)} worth {bestPokemon.BaseExperience} exp!";

                    Embed[] embeds = new Embed[]
                        {
                            new EmbedBuilder()
                            .WithImageUrl(bestPokemon.ImageUrl)
                            .Build()
                        };
                    // Reply in Discord
                    await RespondAsync(message, embeds);
                }
                else
                {
                    // Reply in Discord
                    await RespondAsync($"{username} hasn't caught any Pokémon yet.");
                }
            }
            else
            {
                // Reply in Discord
                Console.WriteLine($"PlayerData not found for {username} {userId}");
                await RespondAsync($"No score available for {username}. Have you caught any Pokémon yet?");
            }
        }
    }
}

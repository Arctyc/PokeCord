using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;

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
                string score = playerData.Experience.ToString("N0");
                string pokemonDollars = playerData.PokemonDollars.ToString("N0");
                List<PokemonData> caughtPokemon = playerData.CaughtPokemon;
                int catches = caughtPokemon.Count;

                // Find best catch
                if (playerData.CaughtPokemon.Any())
                {
                    PokemonData bestPokemon = caughtPokemon.OrderByDescending(p => p.BaseExperience).FirstOrDefault();

                    int averageExp = playerData.Experience / playerData.CaughtPokemon.Count;

                    // Format Discord reply
                    string message = $"{username} has caught {catches} Pokémon totalling {score} exp.\n" +
                                     //$"Rank: \n" +
                                     $"Average exp/catch: {averageExp}\n" +
                                     $"Pokémon Dollars: {pokemonDollars}\n" +
                                     $"They have earned {playerData.EarnedBadges.Count} out of {badges.Count} badges.\n" +
                                     $"Their best catch was this {(bestPokemon.Shiny ? "SHINY " : "")}" +
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

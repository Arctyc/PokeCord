using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    internal class PokebadgesModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ScoreboardService scoreboardService;
        private readonly BadgeService badgeService;

        public PokebadgesModule(IServiceProvider services)
        {
            scoreboardService = services.GetRequiredService<ScoreboardService>();
            badgeService = services.GetRequiredService<BadgeService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokebadges", "View a list of all your earned badges.")]
        public async Task PokebadgesCommand()
        {
            string badgeCountMessage;
            List<Badge> badges = badgeService.GetBadges();

            ulong userId = Context.User.Id;
            string username = Context.User.GlobalName;

            PlayerData playerData = new PlayerData();
            if (scoreboardService.TryGetPlayerData(userId, out playerData))
            {
                Console.WriteLine($"PlayerData found for {username} {userId}");
            }
            else
            {
                // PlayerData does not exist for this userId
                await RespondAsync($"No data for {username} found. Have you caught your first Pokémon?");
            }

            if (playerData.EarnedBadges == null)
            {
                badgeCountMessage = $"{username} has not yet earned any badges.";
            }
            else
            {
                badgeCountMessage = $"{username} has acquired {playerData.EarnedBadges.Count} of {badges.Count} badges.\n";
            }

            foreach (Badge badge in playerData.EarnedBadges)
            {
                badgeCountMessage += string.Join(", ", badge.Name);
            }

            // Reply in Discord
            await RespondAsync(badgeCountMessage);
        }
    }
}

using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    public class PokebadgesModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlayerDataService _playerDataService;
        private readonly BadgeService _badgeService;

        public PokebadgesModule(IServiceProvider services)
        {
            Console.Write("Loaded command: pokebadges\n");
            _playerDataService = services.GetRequiredService<PlayerDataService>();
            _badgeService = services.GetRequiredService<BadgeService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokebadges", "View a list of all your earned badges.")]
        public async Task PokebadgesCommand()
        {
            string badgeCountMessage;
            List<Badge> badges = _badgeService.GetBadges();

            ulong userId = Context.User.Id;
            string username = Context.User.GlobalName;

            PlayerData playerData = await _playerDataService.TryGetPlayerDataAsync(userId);
            if (playerData != null)
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
                badgeCountMessage = $"{username} has acquired {playerData.EarnedBadges.Count} of {badges.Count} badges.\n" +
                                    $"{String.Join(", ", playerData.EarnedBadges.Select(b => b.Name))}";
            }
            // Reply in Discord
            await RespondAsync(badgeCountMessage);
        }
    }
}

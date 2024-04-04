using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.SlashCommands
{
    internal class PokebadgesModule
    {

        /*
        [SlashCommand]
        public async Task PokebadgesCommand(InteractionContext context)
        {
            string badgeCountMessage;

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
            await command.RespondAsync(badgeCountMessage);
        }
        */
    }
}

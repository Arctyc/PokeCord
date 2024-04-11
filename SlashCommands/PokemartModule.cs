using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Linq;

namespace PokeCord.SlashCommands
{
    public class PokemartModule : InteractionModuleBase<SocketInteractionContext>
    {
        private enum PokemartItem
        {
            Menu,
            Pokeballs,
            AmuletCoin,
            ExpShare,
            LuckyEgg,
            ShinyCharm,
            XSpeed
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokemart", "Spend your hard earned Pokémon Dollars on neat items!")]
        public async Task PokemartCommand(
            [Summary("item", "The item you would like to buy.")]
            [Autocomplete(typeof(PokemartItem))] string pokemartItem)
        {
            pokemartItem = pokemartItem.ToLower();
            string message;
            Pokemart pokemart = new Pokemart();

            switch (pokemartItem)
            {
                case "menu":
                    await RespondAsync(await pokemart.GetMenu());
                    return;
                case "pokeballs":
                    message = await pokemart.PurchasePokeballs(Context, pokemartItem);
                    break;
                case "amuletcoin":
                    message = await pokemart.PurchaseAmuletCoin(Context, pokemartItem);
                    break;
                case "expshare":
                    message = await pokemart.PurchaseExpShare(Context, pokemartItem);
                    break;
                case "luckyegg":
                    message = await pokemart.PurchaseLuckyEgg(Context, pokemartItem);
                    break;
                case "shinycharm":
                    message = await pokemart.PurchaseShinyCharm(Context, pokemartItem);
                    break;
                case "xspeed":
                    message = await pokemart.PurchaseXSpeed(Context, pokemartItem);
                    break;
                default:
                    message = $"Sorry, the Poké Mart doesn't sell {pokemartItem}";
                    break;
            }
            // Respond in Discord
            await RespondAsync(message);
        }
    }
}

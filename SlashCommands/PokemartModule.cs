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
        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokemart", "Spend your hard earned Pokémon Dollars on neat items!")]
        public async Task PokemartCommand(
            [Summary("item", "The item you would like to buy.")]
            [Autocomplete(typeof(PokemartAutocompleter))] string pokemartItem)
        {
            string message;
            Pokemart pokemart = new Pokemart();
            Console.WriteLine($"{Context.User.GlobalName} has used /pokemart {pokemartItem}");
            switch (pokemartItem)
            {
                case "My Items":
                    await RespondAsync(await pokemart.GetUserItems(Context), ephemeral: true);
                    return;
                case "Menu":
                    await RespondAsync(await pokemart.GetMenu(), ephemeral: true);
                    return;
                case "Poké Balls":
                    message = await pokemart.PurchasePokeballs(Context, pokemartItem);
                    break;
                case "Amulet Coin":
                    message = await pokemart.PurchaseAmuletCoin(Context, pokemartItem);
                    break;
                case "Exp. Share":
                    message = await pokemart.PurchaseExpShare(Context, pokemartItem);
                    break;
                case "Lucky Egg":
                    message = await pokemart.PurchaseLuckyEgg(Context, pokemartItem);
                    break;
                case "Shiny Charm":
                    message = await pokemart.PurchaseShinyCharm(Context, pokemartItem);
                    break;
                case "X Speed":
                    message = await pokemart.PurchaseXSpeed(Context, pokemartItem);
                    break;
                default:
                    message = $"Sorry, the Poké Mart doesn't sell {pokemartItem}. Use the command options to select an item.";
                    break;
            }
            // Respond in Discord
            await RespondAsync(message);
        }
    }
}

using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.Helpers
{
    public class Pokemart
    {
        private readonly IServiceProvider _services = Program.GetServices();
        private readonly ScoreboardService _scoreboard;
        // Poke Mart Menu
        public const int CostPokeballs = 500;
        public const int AmountPokeballs = 10;
        public const int CostAmuletCoin = 400;
        public const int AmountAmuletCoin = 40;
        public const int CostExpShare = 1500;
        public const int AmountExpShare = 10;
        public const int CostLuckyEgg = 500;
        public const int AmountLuckyEgg = 25;
        public const int CostShinyCharm = 1000;
        public const int AmountShinyCharm = 1;
        public const int CostXSpeed = 500;
        public const int AmountXSpeed = 10;

        public Pokemart()
        {
            _scoreboard = _services.GetRequiredService<ScoreboardService>();
        }
        public async Task<String> GetMenu()
        {
            return $"Poké Balls: {AmountPokeballs} for P{CostPokeballs}\n" +
                   $"Amulet Coin - 2x Pokémon Dollars from catches: {AmountAmuletCoin} charges for P{CostAmuletCoin}\n" +
                   $"Exp. Share - Team members also gain weekly Exp. from your catches: {AmountExpShare} charges for P{CostExpShare}\n" +
                   $"Lucky Egg - 2x Exp. from low Exp. Pokémon: {AmountLuckyEgg} charges for P{CostLuckyEgg}\n" +
                   $"Shiny Charm - 2x shiny chance until consumed. {AmountShinyCharm} for P{CostShinyCharm}\n" +
                   $"X Speed - Half cooldown time. {AmountXSpeed} charges for P{CostXSpeed}\n" +
                   "You may only have 1 of each item at a time.";
        }

        public async Task<string> GetUserItems(SocketInteractionContext context)
        {
            string username = context.User.GlobalName;
            ulong userId = context.User.Id;
            string message = string.Empty;
            if (_scoreboard.TryGetPlayerData(userId, out var playerData))
            {
                message += $"You have {playerData.PokemonDollars} Pokémon Dollars and the following items:\n";
                foreach(var key in playerData.PokeMartItems)
                {
                    message += $"{key}";
                }
            }
            else
            {
                message = $"Error accessing player data.";
            }            
            return message;
        }
        public async Task<String> PurchasePokeballs(SocketInteractionContext context, string key)
        {
            string message;
            string username = context.User.GlobalName;
            ulong userId = context.User.Id;
            // Get player data
            if (_scoreboard.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostPokeballs)
                {
                    return $"You need {CostPokeballs - playerData.PokemonDollars} more Pokémon Dollars to purchase Poké Balls.";
                }
                else // Complete transaction
                {
                    playerData.PokemonDollars -= CostPokeballs;
                    playerData.Pokeballs += AmountPokeballs;
                }
                // Save
                await _scoreboard.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased {AmountPokeballs} Poké Balls!";
            }
            else
            {
                return $"Error accessing player data. You have not been charged.";
            }
            return message;
        }

        public async Task<string> PurchaseAmuletCoin(SocketInteractionContext context, string key)
        {
            return await PurchaseItem(
                context,
                key,
                CostAmuletCoin,
                AmountAmuletCoin,
                "an Amulet Coin");
        }

        public async Task<String> PurchaseExpShare(SocketInteractionContext context, string key)
        {
            return await PurchaseItem(
                context,
                key,
                CostExpShare,
                AmountExpShare,
                "an Exp. Share");
        }

        public async Task<String> PurchaseLuckyEgg(SocketInteractionContext context, string key)
        {
            return await PurchaseItem(
                context,
                key,
                CostLuckyEgg,
                AmountLuckyEgg,
                "a Lucky Egg");
        }

        public async Task<String> PurchaseShinyCharm(SocketInteractionContext context, string key)
        {
            return await PurchaseItem(
                context,
                key,
                CostShinyCharm,
                AmountShinyCharm,
                "a Shiny Charm");
        }

        public async Task<String> PurchaseXSpeed(SocketInteractionContext context, string key)
        {
            return await PurchaseItem(
                context,
                key,
                CostXSpeed,
                AmountXSpeed,
                "an X Speed");
        }

        private async Task<string> PurchaseItem(SocketInteractionContext context, string itemKey, int itemCost, int itemCharges, string richItemName)
        {
            string username = context.User.GlobalName;
            ulong userId = context.User.Id;
            PlayerData originalPlayerData = new PlayerData();
            if (_scoreboard.TryGetPlayerData(userId, out originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;

                // Check if player already has item
                playerData.PokeMartItems.TryGetValue(itemKey, out int onHand);
                if (onHand > 0)
                {
                    return $"You still have {onHand} charges on this item. Please use those before purchasing another.";
                }

                // Check that player can afford item
                if (playerData.PokemonDollars < itemCost)
                {
                    return $"You need {itemCost - playerData.PokemonDollars} more Pokémon Dollars to purchase this item.";
                }
                
                // Complete Transaction
                playerData.PokemonDollars -= itemCost;
                playerData.PokeMartItems[itemKey] = itemCharges;

                // Save
                await _scoreboard.SavePlayerDataAsync(playerData, originalPlayerData);
                await _scoreboard.SaveScoreboardAsync();
                return $"{username} has purchased {richItemName}!";
            }
            else
            {
                return $"Error accessing player data. You have not been charged.";
            }
        }
    }
}
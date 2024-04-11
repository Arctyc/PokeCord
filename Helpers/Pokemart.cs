﻿using Discord.Interactions;
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
        private readonly ScoreboardService scoreboardService;
        // Poke Mart Menu
        private const int CostPokeballs = 250;
        private const int AmountPokeballs = 10;
        private const int CostAmuletCoin = 500;
        private const int AmountAmuletCoin = 50;
        private const int CostExpShare = 1000;
        private const int AmountExpShare = 10;
        private const int CostLuckyEgg = 500;
        private const int AmountLuckyEgg = 5;
        private const int CostShinyCharm = 1000;
        private const int AmountShinyCharm = 1;
        private const int CostXSpeed = 250;
        private const int AmountXSpeed = 10;

        public async Task<String> GetMenu()
        {
            return $"Poké Balls: {AmountPokeballs} for P{CostPokeballs}\n" +
                   $"Amulet Coin - 2x Pokémon Dollars from catches: {AmountAmuletCoin} charges for P{CostAmuletCoin}\n" +
                   $"Exp. Share - Team members also gain Exp. from your catches: {AmountExpShare} charges for P{CostExpShare}\n" +
                   $"Lucky Egg - 2x Exp. from low Exp. Pokémon: {AmountLuckyEgg} charges for P{CostLuckyEgg}\n" +
                   $"Shiny Charm - 2x shiny chance until consumed. {AmountShinyCharm} for P{CostShinyCharm}\n" +
                   $"X Speed - Half cooldown time. {AmountXSpeed} charges for P{CostXSpeed}\n" +
                   "You may only have 1 of each item at a time.";
        }

        public async Task<String> PurchasePokeballs(SocketInteractionContext Context, string key)
        {
            string message;
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            // Get player data
            if (scoreboardService.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostPokeballs)
                {
                    return $"You need {playerData.PokemonDollars - CostPokeballs} more Pokémon Dollars to purchase Poké Balls.";
                }

                // Charge cost
                playerData.PokemonDollars -= CostPokeballs;
                // Add item to player's inventory
                playerData.Pokeballs += AmountPokeballs;

                await scoreboardService.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased Poké Balls!";
            }
            else
            {
                return $"There was an error in the Poké Mart. You have not been charged.";
            }
            return message;
        }

        public async Task<String> PurchaseAmuletCoin(SocketInteractionContext Context, string key)
        {
            string message;
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            // Get player data
            if (scoreboardService.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostAmuletCoin)
                {
                    return $"You need {playerData.PokemonDollars - CostAmuletCoin} more Pokémon Dollars to purchase an Amulet Coin.";
                }

                // Check player doesn't already have item
                playerData.PokeMartItems.TryGetValue(key, out int onHand);
                if (onHand > 0)
                {
                    return "You need to use your * before you can purchase more";
                }

                // Charge cost
                playerData.PokemonDollars -= CostAmuletCoin;

                // Add item to player's inventory
                

                await scoreboardService.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased an Amulet Coin!";
            }
            else
            {
                message = $"There was an error in the Poké Mart. You have not been charged.";
            }
            return message;
        }

        public async Task<String> PurchaseExpShare(SocketInteractionContext Context, string key)
        {
            string message;
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            // Get player data
            if (scoreboardService.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostExpShare)
                {
                    return $"You need {playerData.PokemonDollars - CostExpShare} more Pokémon Dollars to purchase an Exp. Share.";
                }

                // Check player doesn't already have item
                playerData.PokeMartItems.TryGetValue(key, out int onHand);
                if (onHand > 0)
                {
                    return "You need to use your * before you can purchase more";
                }
                // Charge cost
                playerData.PokemonDollars -= CostExpShare;

                // Add item to player's inventory


                await scoreboardService.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased an Exp. Share!";
            }
            else
            {
                message = $"There was an error in the Poké Mart. You have not been charged.";
            }
            return message;
        }

        public async Task<String> PurchaseLuckyEgg(SocketInteractionContext Context, string key)
        {
            string message;
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            // Get player data
            if (scoreboardService.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostLuckyEgg)
                {
                    return $"You need {playerData.PokemonDollars - CostLuckyEgg} more Pokémon Dollars to purchase a Lucky Egg.";
                }

                // Check player doesn't already have item
                playerData.PokeMartItems.TryGetValue(key, out int onHand);
                if (onHand > 0)
                {
                    return "You need to use your * before you can purchase more";
                }

                // Charge cost
                playerData.PokemonDollars -= CostLuckyEgg;

                // Add item to player's inventory
                // TODO: left off here
                // check dict for key, if not, add, either way, update value

                await scoreboardService.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased a Lucky Egg!";
            }
            else
            {
                message = $"There was an error in the Poké Mart. You have not been charged.";
            }
            return message;
        }

        public async Task<String> PurchaseShinyCharm(SocketInteractionContext Context, string key)
        {
            string message;
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            // Get player data
            if (scoreboardService.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostShinyCharm)
                {
                    return $"You need {playerData.PokemonDollars - CostShinyCharm} more Pokémon Dollars to purchase a Shiny Charm.";
                }

                // Check player doesn't already have item
                playerData.PokeMartItems.TryGetValue(key, out int onHand);
                if (onHand > 0)
                {
                    return "You need to use your * before you can purchase more";
                }

                // Charge cost
                playerData.PokemonDollars -= CostShinyCharm;

                // Add item to player's inventory


                await scoreboardService.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased a Shiny Charm!";
            }
            else
            {
                message = $"There was an error in the Poké Mart. You have not been charged.";
            }
            return message;
        }

        public async Task<String> PurchaseXSpeed(SocketInteractionContext Context, string key)
        {
            string message;
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            // Get player data
            if (scoreboardService.TryGetPlayerData(userId, out PlayerData originalPlayerData))
            {
                PlayerData playerData = originalPlayerData;
                // Check player for funds
                if (playerData.PokemonDollars < CostXSpeed)
                {
                    return $"You need {playerData.PokemonDollars - CostXSpeed} more Pokémon Dollars to purchase an X Speed.";
                }

                // Check player doesn't already have item
                playerData.PokeMartItems.TryGetValue(key, out int onHand);
                if (onHand > 0)
                {
                    return "You need to use your * before you can purchase more";
                }

                // Charge cost
                playerData.PokemonDollars -= CostXSpeed;

                // Add item to player's inventory


                await scoreboardService.SavePlayerDataAsync(playerData, originalPlayerData);
                message = $"{username} has purchased an X Speed!";
            }
            else
            {
                message = $"There was an error in the Poké Mart. You have not been charged.";
            }
            return message;
        }
    }
}

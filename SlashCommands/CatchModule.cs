﻿using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Collections.Concurrent;
using System.Timers;


namespace PokeCord.SlashCommands
{
    public class CatchModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ScoreboardService _scoreboard;
        private readonly BadgeService _badgeService;
        private readonly PokeApiClient _pokeApiClient;

        // TODO: These should probably be in a config file
        const int maxPokemonId = 1025; // Highest Pokemon ID to be requested on PokeApi
        const int shinyRatio = 256; // Chance of catching a shiny
        private const int pokemonDollarRatio = 10; // % to divide base exp by for awarding pokemon dollars
        private const int currencyCap = 5000; // Maximum amount of pokemondollars a player can have

        public const string amuletCoinKey = "Amulet Coin";
        public const int amuletCoinMultiplier = 2;
        public const string expShareKey = "Exp. Share";
        public const string luckyEggKey = "Lucky Egg";
        public const int luckyEggMultiplier = 2;
        public const int luckyEggMaximumExp = 100;
        public const string shinyCharmKey = "Shiny Charm";
        public const string xSpeedKey = "X Speed";

        //Cooldown data structure
        public static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = Program._lastCommandUsage;
        public static readonly TimeSpan _standardCooldownTime = Program._standardCooldown; // Cooldown time in seconds (120)
        public static readonly TimeSpan _xSpeedCooldownTime = Program._xSpeedCooldown; // X Speed cooldown time in seconds (60)

        public CatchModule(IServiceProvider services)
        {
            Console.Write("\n----------Loaded command: catch\n");
            _scoreboard = services.GetRequiredService<ScoreboardService>();
            _badgeService = services.GetRequiredService<BadgeService>();
            _pokeApiClient = services.GetRequiredService<PokeApiClient>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("catch", "Catch a Pokémon!")]
        public async Task CatchCommand()
        {
            string username = Context.User.GlobalName;
            ulong userId = Context.User.Id;
            int xSpeedCharges = -1; // Initialize to a negative number, updated later if player has X Speed

            PlayerData originalPlayerData = new PlayerData();
            List<Badge> badges = _badgeService.GetBadges();
            Console.WriteLine($"[{DateTime.UtcNow.ToString("HH:mm:ss")}] {username} used catch");

            // Get the PlayerData instance from the scoreboard
            PlayerData playerData = new PlayerData();
            if (_scoreboard.TryGetPlayerData(userId, out originalPlayerData))
            {
                // PlayerData exists for this userId
                playerData = originalPlayerData;
                Console.WriteLine($"Got PlayerData for {username}");
            }
            else
            {
                // PlayerData does not exist for this userId, create new
                // TODO: Update this with new player data each version. Current version: 4
                playerData = new PlayerData
                {
                    UserId = userId,
                    UserName = username,
                    Experience = 0,
                    WeeklyExperience = 0,
                    TeamId = -1,
                    Pokeballs = ScoreboardService.pokeballRestockAmount,
                    CaughtPokemon = new List<PokemonData>(),
                    WeeklyCaughtPokemon = new List<PokemonData>(),
                    EarnedBadges = new List<Badge>(),
                    PokeMartItems = new Dictionary<string, int>() // New

                };
                if (_scoreboard.TryAddPlayerData(userId, playerData))
                {
                    originalPlayerData = playerData;
                    Console.WriteLine($"New PlayerData for {username} created with userId {userId}");
                }
                else
                {
                    Console.WriteLine($"Unable to create PlayerData for {username} with userId {userId}");
                    await RespondAsync($"Something went wrong setting up your trainer profile.");
                }
            }

            // Check for enough Pokeballs
            if (playerData.Pokeballs <= 0)
            {
                // Get time until next restock
                TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
                int timeRemaining = (int)delay.TotalSeconds;
                var cooldownUnixTime = (long)(DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                await RespondAsync($"Sorry, you're out of Poké Balls for now. " +
                    $"The Poké Mart will automatically send you up to {ScoreboardService.pokeballRestockAmount} new Poké Balls <t:{cooldownUnixTime}:R>. " +
                    $"Unfortunately, you will not receive a bonus Premier Ball.");
                return;
            }

            // Check if player has items
            bool hasAmuletCoin = playerData.PokeMartItems.TryGetValue(amuletCoinKey, out int amuletCoinCharges);
            bool hasExpShare = playerData.PokeMartItems.TryGetValue(expShareKey, out int expShareCharges);
            bool hasLuckyEgg = playerData.PokeMartItems.TryGetValue(luckyEggKey, out int luckyEggCharges);
            bool hasShinyCharm = playerData.PokeMartItems.TryGetValue(shinyCharmKey, out int shinyCharmCharges);
            bool hasXSpeed = playerData.PokeMartItems.TryGetValue(xSpeedKey, out xSpeedCharges);

            // NEW COOLDOWN CHECK
            TimeSpan elapsed;
            TimeSpan playerCDT;
            (bool notFirstCatch, DateTime lastUsed) = GetLastUsedTime(userId);
            if (notFirstCatch)
            {
                (elapsed, playerCDT) = GetPlayerCooldown(userId, username, hasXSpeed, xSpeedCharges);

                // Bailout on repeated command
                if (elapsed < TimeSpan.FromSeconds(5))
                {
                    await RespondAsync($"PokeCord experienced a connection error causing discord to issue repeated commands. " +
                        $"Please wait a few seconds and try again. If you see this message more than once, ping ArctycFox.");
                    return;
                }

                if (elapsed < playerCDT)
                {
                    int timeRemaining = (int)playerCDT.TotalSeconds - (int)elapsed.TotalSeconds;
                    var cooldownUnixTime = (long)DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    Console.WriteLine($"{username} catch denied. Cooldown: {timeRemaining} seconds");
                    await RespondAsync($"Easy there, Ash Ketchum! I know you Gotta Catch 'Em All, " +
                                       $"but your next Poké Ball will be available <t:{cooldownUnixTime}:R>.",
                                       ephemeral: true);
                    return;
                }
            }            
            else
            {
                Console.WriteLine($"No last command usage by {username} with userID {userId}");
            }
            // Player is not on cooldown
            UpdatePlayerCooldown(userId, username, lastUsed);

            // Generate a new pokemon
            PokeSelector pokeSelector = new PokeSelector();
            PokemonData pokemonData = await pokeSelector.GetRandomPokemon(_pokeApiClient, playerData);

            if (pokemonData != null)
            {
                Console.WriteLine($"{username} caught a {(pokemonData.Shiny ? "shiny " : "")}{pokemonData.Name} #{pokemonData.PokedexId}");
                int pokemonExperienceValue = (int)pokemonData.BaseExperience;
                int pokemonDollarValue = pokemonExperienceValue / pokemonDollarRatio;
                int adjustedPokemonDollarValue = pokemonDollarValue;

                List<string> consumptionMessages = new List<string>();
                // Consume Amulet Coin
                if (hasAmuletCoin && amuletCoinCharges > 0)
                {
                    Console.WriteLine($"{amuletCoinKey} used by {username}");
                    adjustedPokemonDollarValue *= amuletCoinMultiplier;
                    amuletCoinCharges--;
                    playerData.PokeMartItems[amuletCoinKey]--;
                }
                if (hasAmuletCoin && amuletCoinCharges == 0)
                {
                    playerData.PokeMartItems.Remove(amuletCoinKey);
                    string conMessage = $"{amuletCoinKey} consumed. 💔";
                    consumptionMessages.Add(conMessage);
                    Console.WriteLine($"{amuletCoinKey} consumed by {username}");
                }             
                // Consume Lucky Egg
                if (hasLuckyEgg && luckyEggCharges > 0)
                {
                    if (pokemonExperienceValue < luckyEggMaximumExp)
                    {
                        Console.WriteLine($"{luckyEggKey} used by {username}");
                        pokemonExperienceValue *= luckyEggMultiplier;
                        luckyEggCharges--;
                        playerData.PokeMartItems[luckyEggKey]--;
                    }
                }
                if (hasLuckyEgg && luckyEggCharges == 0)
                {
                    playerData.PokeMartItems.Remove(luckyEggKey);
                    string conMessage = $"{luckyEggKey} consumed. 💔";
                    consumptionMessages.Add(conMessage);
                    Console.WriteLine($"{luckyEggKey} consumed by {username}");
                }
                // Consume Exp. Share
                if (hasExpShare && expShareCharges > 0 && playerData.TeamId > 0)
                {
                    Console.WriteLine($"{expShareKey} used by {username}");
                    GiveExpToTeamMembers(userId, playerData.TeamId, pokemonExperienceValue);
                    expShareCharges--;
                    playerData.PokeMartItems[expShareKey]--;
                }
                if (hasExpShare && expShareCharges == 0)
                {
                    playerData.PokeMartItems.Remove(expShareKey);
                    string conMessage = $"{expShareKey} consumed. 💔";
                    consumptionMessages.Add(conMessage);
                    Console.WriteLine($"{expShareKey} consumed by {username}");
                }
                // Consume Shiny Charm
                if (hasShinyCharm && pokemonData.Shiny)
                {
                    Console.WriteLine($"{shinyCharmKey} used by {username}");
                    playerData.PokeMartItems.Remove(shinyCharmKey);
                    string conMessage = $"{shinyCharmKey} consumed. 💔";
                    consumptionMessages.Add(conMessage);
                    Console.WriteLine($"{shinyCharmKey} consumed by {username}");
                }
                // Consume X Speed
                if (hasXSpeed && xSpeedCharges > 0)
                {
                    Console.WriteLine($"{xSpeedKey} used by {username}");
                    xSpeedCharges--;
                    playerData.PokeMartItems[xSpeedKey]--; // Remove 1 charge                    
                    Console.WriteLine($"{xSpeedKey} used for {username}. {xSpeedCharges} charges remaining.");
                }
                if (hasXSpeed && xSpeedCharges == 0) // If this was the last X Speed, remove the key.
                {
                    // Remove X Speed
                    playerData.PokeMartItems.Remove(xSpeedKey);
                    hasXSpeed = false;
                    string conMessage = $"{xSpeedKey} consumed. 💔";
                    consumptionMessages.Add(conMessage);
                    Console.WriteLine($"Removed {xSpeedKey} from {username}");
                }

                // Check if new
                var isCaught = playerData.CaughtPokemon.Any(p => p.PokedexId == pokemonData.PokedexId);
                Console.WriteLine($"{username}'s value for isCaught on {pokemonData.Name}: {isCaught}");

                Console.WriteLine($"Attempting to update playerData object for {username}");
                // Update the existing playerData instance
                playerData.Experience += pokemonExperienceValue;// Award overall experience points
                playerData.WeeklyExperience += pokemonExperienceValue;// Award weekly experience points
                playerData.Pokeballs -= 1; // subtract one pokeball from user's inventory
                if (pokemonData.Shiny) { playerData.Pokeballs += 10; } // Add 10 Pokeballs for shiny
                playerData.PokemonDollars += adjustedPokemonDollarValue; // award pokemon dollars
                if (playerData.PokemonDollars > currencyCap) { playerData.PokemonDollars = currencyCap; } // Cap player pokemondollars
                playerData.CaughtPokemon.Add(pokemonData); // Add the pokemon to the player's list of caught pokemon
                playerData.WeeklyCaughtPokemon.Add(pokemonData); // Add the pokemon to the player's weekly list of caught pokemon
                

                // Check for new badges
                Console.WriteLine($"Adding new badges (if any) for {username}");
                BadgeManager badgeManager = new BadgeManager();
                List<Badge> newBadges = badgeManager.UpdateBadgesAsync(playerData, badges, pokemonData);
                List<string> newBadgeMessages = new List<string>();
                if (newBadges != null)
                {
                    foreach (Badge badge in newBadges)
                    {
                        // Add badges to playerData
                        playerData.EarnedBadges.Add(badge);
                        playerData.Pokeballs += badge.BonusPokeballs;

                        string newBadgeMessage = $"{username} has acquired the {badge.Name}! +{badge.BonusPokeballs} Poké Balls!\n" +
                                                 $"{badge.Description}";
                        newBadgeMessages.Add(newBadgeMessage);
                    }
                }

                // Update Scoreboard in memory
                if (_scoreboard.TryUpdatePlayerData(userId, playerData, originalPlayerData))
                {
                    Console.WriteLine($"Catch written to scoreboard for {username}'s {pokemonData.Name}");
                }
                else
                {
                    Console.WriteLine($"Failed to write catch to scoreboard for {username}'s {pokemonData.Name}");
                }
                
                // Save the updated scoreboard data
                await _scoreboard.SaveScoreboardAsync();

                // Clean up output variables
                string richPokemonName = CleanOutput.FixPokemonName(pokemonData.Name);
                bool startsWithVowel = "aeiouAEIOU".Contains(richPokemonName[0]);
                if (pokemonData.Shiny) { startsWithVowel = false; }

                // Get team name if player is on team
                List<Team> allTeams = _scoreboard.GetTeams();
                bool onTeam = false;
                string playerTeam = "";
                if (playerData.TeamId != -1)
                {
                    onTeam = true;
                    playerTeam = allTeams.FirstOrDefault(t => t.Id == playerData.TeamId).Name;
                }

                // Format Discord output
                string message = $"{(onTeam ? $"[Team {playerTeam}] {username}" : $"{username}")} caught {(startsWithVowel ? "an" : "a")} " +
                                 $"{(pokemonData.Shiny ? ":sparkles:SHINY:sparkles: " : "")}{richPokemonName}!{(isCaught ? "" : " 🆕")}" +
                                 $"{(pokemonData.Shiny ? " +10 Poké Balls!" : "")}\n" +
                                 $"+{pokemonExperienceValue} {(pokemonExperienceValue != pokemonData.BaseExperience ? $"({pokemonData.BaseExperience} x2) " : "")}Exp. " +
                                 $"+{adjustedPokemonDollarValue} {(adjustedPokemonDollarValue != pokemonDollarValue ? $"({pokemonDollarValue} x2) " : "")}Pokémon Dollars.";
                Embed[] embeds = new Embed[]
                {
                            new EmbedBuilder()
                            .WithImageUrl(pokemonData.ImageUrl)
                            .Build()
                };

                // Append additional messages
                if (newBadgeMessages.Count > 0)
                {
                    Console.WriteLine($"Appending new badge messages");
                    message = AppendListMessages(newBadgeMessages, message);
                }
                if (consumptionMessages.Count > 0)
                {
                    Console.WriteLine($"Appending consumption messages");
                    message = AppendListMessages(consumptionMessages, message);
                }
                // Append pokeballs remaining with += to avoid having to pass the message.
                message += AppendPokeballsRemaining(playerData);

                message += AppendNextCatch(userId, username, hasXSpeed, xSpeedCharges);

                // Send Discord reply
                await RespondAsync(message, embeds);
            }
            else
            {
                await RespondAsync("Error catching a Pokémon :( @arctycfox What's up?");
                Console.WriteLine($"{username}'s command failed at " + DateTime.Now.ToString());
            }
        }

        private string AppendListMessages(List<string> messages, string originalMessage)
        {
            string newMessage = String.Join("\n", messages);
            originalMessage += "\n" + newMessage;
            return originalMessage;
        }

        private string AppendPokeballsRemaining(PlayerData playerData)
        {
            return $"\n{playerData.Pokeballs} Poké Ball{(playerData.Pokeballs == 1 ? "" : "s")} remaining.";
        }

        private string AppendNextCatch(ulong userId, string username, bool hasXSpeed, int xSpeedCharges)
        {
            (TimeSpan elapsed, TimeSpan playerCDT) = GetPlayerCooldown(userId, username, hasXSpeed, xSpeedCharges);
            int timeRemaining = (int)playerCDT.TotalSeconds - (int)elapsed.TotalSeconds;
            var cooldownUnixTime = (long)DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return $" Next catch <t:{cooldownUnixTime}:R>.";
        }

        private void UpdatePlayerCooldown(ulong userId, string username, DateTime lastUsed)
        {
            // Try to add a new lastUsed time for the user, if it returns false, it exists, so update
            if (!_lastCommandUsage.TryAdd(userId, DateTime.UtcNow))
            {
                //Cooldown exists so update existing cooldown
                if (_lastCommandUsage.TryUpdate(userId, DateTime.UtcNow, lastUsed))
                {
                    Console.WriteLine($"Cooldown updated for {username}");
                }
                else
                {
                    Console.WriteLine($"Unable to update cooldown for {username} with data {userId}:{DateTime.UtcNow}");
                }
            }
        }

        private (TimeSpan, TimeSpan) GetPlayerCooldown(ulong userId, string username, bool hasXSpeed, int xSpeedCharges)
        {
            (bool notFirstCatch, DateTime lastUsed) = GetLastUsedTime(userId);
            if (notFirstCatch)
            {
                Console.WriteLine($"Cooldown entry read: key {username} value {lastUsed}");
                TimeSpan elapsed = DateTime.UtcNow - lastUsed;
                TimeSpan playerCDT = _standardCooldownTime;

                // Check if player has X Speed
                if (hasXSpeed)
                {
                    Console.WriteLine($"{username} has {xSpeedCharges} {xSpeedKey} charges.");
                    playerCDT = _xSpeedCooldownTime; // Set player to the X Speed cooldown
                }
                return (elapsed, playerCDT);
            }
            else
            {
                Console.WriteLine($"No last command usage by {username} with userID {userId}");
                return (TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(-1));
            }
        }

        private (bool, DateTime) GetLastUsedTime(ulong userId)
        {
            if (_lastCommandUsage.TryGetValue(userId, out DateTime lastUsed))
            {
                return (true, lastUsed);
            }
            else
            {
                return (false, DateTime.MinValue);
            }
        }

        private void GiveExpToTeamMembers(ulong user, int teamId, int pokemonExperience)
        {
            var team = _scoreboard.GetTeams().FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                Console.WriteLine($"** GiveExpToTeamMembers called for {user}");
                var otherTeamMembers = team.Players.Where(p => p != user);
                int sharedExp = pokemonExperience / otherTeamMembers.Count(); // Split 1x exp among other team members
                foreach (var player in otherTeamMembers)
                {
                    if (_scoreboard.TryGetPlayerData(player, out var playerData))
                    {
                        playerData.WeeklyExperience += sharedExp;
                        _scoreboard.TryUpdatePlayerData(player, playerData, playerData);
                        Console.WriteLine($"Added {sharedExp} Exp to {playerData.UserName} due to {expShareKey}");
                    }
                }
            }
        }
    }
}

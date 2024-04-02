using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Discord;
using Discord.Net;
using Discord.Interactions;
using Discord.WebSocket;

using PokeApiNet;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Reflection.Metadata;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PokeCord
{
    public class Program
    {
        const int maxPokemonId = 1025; // Highest Pokemon ID to be requested on PokeApi
        const int shinyRatio = 256; // Chance of catching a shiny
        private const int pokeballMax = 50; // Maximum catches per restock (currently hourly)
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;
        private static Timer _pokeballResetTimer;
        private static TimeSpan delay;

        //TODO: Create a timer to batch save to file every so often

        //TODO: Monthly leaderboard

        //Cooldown data structure
        private static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        private static readonly TimeSpan _cooldownTime = TimeSpan.FromSeconds(120); // Cooldown time in seconds

        //Scoreboard data structure
        private static ConcurrentDictionary<ulong, PlayerData> scoreboard;
        private static List<Badge> badges;

        public static async Task Main(string[] args)
        {
            // Load badges
            badges = LoadBadges();
            // Load scoreboard
            scoreboard = LoadScoreboard();

            // Remove duplicate badges
            scoreboard = RemoveDuplicateBadges(scoreboard);

            // FETCH ENVIRONMENT VARIABLE TOKEN
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            //var token = Environment.GetEnvironmentVariable("DISCORD_TESTING_TOKEN");

            // Set up Discord.NET
            _client = new DiscordSocketClient();
            _services = ConfigureServices();
            _client.Log += Log;

            // -- Daily Restock
            // Calculate the time remaining until the next pokeball restock
            delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            _pokeballResetTimer = new Timer(async (e) => await ResetPokeballs(null), null, delay, TimeSpan.FromDays(1));
            Console.WriteLine("Time until Pokeball reset: " + delay);

            /*
            // -- Hourly Restock
            // Set up Pokemart
            // Calculate the time remaining until the next pokeball restock (Hourly)
            DateTime nextHour = DateTime.Now.AddHours(1).AddMinutes(-DateTime.Now.Minute).AddSeconds(-DateTime.Now.Second);
            TimeSpan delay = nextHour - DateTime.Now;
            // Call the restock method on the delay
            _pokeballResetTimer = new Timer(async (e) => await ResetPokeballs(null), null, delay, TimeSpan.FromHours(1));
            Console.WriteLine("Time until Pokeball reset: " + delay);
            */

            // Login to Discord
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Set up interactions
            _client.SlashCommandExecuted += SlashCommandHandler;

            // Additionall ready settings
            _client.Ready += ClientReady;

            // Keep bot running indefinitely
            await Task.Delay(Timeout.Infinite);
        }

        public static async Task ClientReady()
        {
            // Make sure everyone has a full stock of pokeballs when bot comes online
            //await ResetPokeballs(null); // Unnecessary for now

            // Set up slash commands
            var catchCommand = new SlashCommandBuilder()
                .WithName("catch")
                .WithDescription("Catch a Pokémon!");

            var scoreCommand = new SlashCommandBuilder()
                .WithName("pokescore")
                .WithDescription("View your PokeCord score.");

            var leaderboardCommand = new SlashCommandBuilder()
                .WithName("pokeleaderboard")
                .WithDescription("Show a list of the trainers with the most exp.");
            var badgesCommand = new SlashCommandBuilder()
                .WithName("pokebadges")
                .WithDescription("Show a list of your earned badges");

            try
            {
                await _client.CreateGlobalApplicationCommandAsync(catchCommand.Build());
                Console.WriteLine("Created command: catch");
                await _client.CreateGlobalApplicationCommandAsync(scoreCommand.Build());
                Console.WriteLine("Created command: pokescore");
                await _client.CreateGlobalApplicationCommandAsync(leaderboardCommand.Build());
                Console.WriteLine("Created command: pokeleaderboard");
                await _client.CreateGlobalApplicationCommandAsync(badgesCommand.Build());
                Console.WriteLine("Created command: pokebadges");

            }
            catch (HttpException ex)
            {
                Console.WriteLine("Could not create a command. " + ex.Message);
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<PokeApiClient>(); // Add PokeApiClient as Singleton
            //services.AddScoped<PokeSelector>(); // Add PokeSelector as Scoped service

            return services.BuildServiceProvider(); // Build and return the service provider
        }

        private static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            string username = command.User.GlobalName;
            ulong userId = command.User.Id;
            PlayerData originalPlayerData = new PlayerData();
            Console.WriteLine($"{username} used {command.Data.Name}");

            // Get the PlayerData instance from the scoreboard
            PlayerData playerData = new PlayerData();
            if (scoreboard.TryGetValue(userId, out originalPlayerData))
            {
                // PlayerData exists for this userId
                playerData = originalPlayerData;
                Console.WriteLine($"PlayerData found for {username} {userId}");
            }
            else
            {
                // PlayerData does not exist for this userId, create new
                playerData = new PlayerData
                {
                    UserId = userId,
                    UserName = username,
                    Experience = 0,
                    Pokeballs = pokeballMax,
                    CaughtPokemon = new List<PokemonData>(),
                    EarnedBadges = new List<Badge>()
                    //Badges = new Dictionary<Badge, DateTime>()
                };
                if (scoreboard.TryAdd(userId, playerData))
                {
                    originalPlayerData = playerData;
                    Console.WriteLine($"New PlayerData for {username} added with userId {userId}");
                }
                else
                {
                    Console.WriteLine($"Unable to add PlayerData for {username} with userId {userId}");
                    await command.RespondAsync($"Something went wrong setting up your player profile.");
                }
            }

            //TODO: Update to switch on command.CommandName, split each command into a unique class

            // Catch command section
            if (command.CommandName == "catch")
            {
                // Check cooldown information
                if (_lastCommandUsage.TryGetValue(userId, out DateTime lastUsed))
                {
                    Console.WriteLine($"{username} cooldown entry read: key {username} value {lastUsed}");
                    TimeSpan elapsed = DateTime.UtcNow - lastUsed;
                    if (elapsed < _cooldownTime)
                    {
                        int timeRemaining = (int)_cooldownTime.TotalSeconds - (int)elapsed.TotalSeconds;
                        var cooldownUnixTime = (long)(DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        Console.WriteLine($"{username} catch denied. Cooldown: {cooldownUnixTime} seconds");
                        await command.RespondAsync($"Easy there, Ash Ketchum! I know you Gotta Catch 'Em All, " +
                                                   $"but your next Poké Ball will be available <t:{cooldownUnixTime}:R>.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"No last command usage by {username} with userID {userId}");
                }
                // If unable to add new cooldown for user
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
                Console.WriteLine($"{username} cooldown entry update attempted: key {username} value {DateTime.UtcNow}");

                // Check for enough Pokeballs
                if (playerData.Pokeballs > 0)
                {
                    // Set up a new PokeSelector
                    PokeSelector pokeSelector = new PokeSelector(maxPokemonId, shinyRatio);
                    // Set up PokeApiClient
                    var pokeApiClient = _services.GetRequiredService<PokeApiClient>();
                    // Get a new pokemon
                    PokemonData pokemonData = await pokeSelector.GetRandomPokemon(pokeApiClient);

                    if (pokemonData != null)
                    {
                        Console.WriteLine($"{username} caught a {(pokemonData.Shiny ? "shiny " : "")}{pokemonData.Name} #{pokemonData.PokedexId}");

                        // Update the existing playerData instance
                        playerData.Experience += (int)pokemonData.BaseExperience;
                        playerData.CaughtPokemon.Add(pokemonData); // Add the pokemon to the player's list of caught pokemon
                        playerData.Pokeballs -= 1; // subtract one pokeball from user's inventory

                        // Check for new badges
                        BadgeManager badgeManager = new BadgeManager();
                        List<Badge> newBadges = badgeManager.UpdateBadgesAsync(playerData, badges, pokemonData);
                        List<string> newBadgeMessages = new List<string>();
                        if (newBadges != null)
                        {
                            foreach (Badge badge in newBadges)
                            {
                                // Add badges to playerData
                                playerData.EarnedBadges.Add(badge);
                                //playerData.Badges.Add(badge, DateTime.UtcNow);

                                string newBadgeMessage = $"{username} has acquired the {badge.Name}!\n{badge.Description}\n";
                                newBadgeMessages.Add(newBadgeMessage);
                            }
                        }

                        if (scoreboard.TryUpdate(userId, playerData, originalPlayerData))
                        {
                            Console.WriteLine($"Catch written to scoreboard for {username}'s {pokemonData.Name}");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to write catch to scoreboard for {username}'s {pokemonData.Name}");
                        }
                        // Save the updated scoreboard data
                        await SaveScoreboardAsync();

                        // Format Discord Reply
                        string richPokemonName = FixPokemonName(pokemonData.Name);
                        bool startsWithVowel = "aeiouAEIOU".Contains(richPokemonName[0]);
                        if (pokemonData.Shiny) { startsWithVowel = false; }
                        string message = $"{username} caught {(startsWithVowel ? "an" : "a")} {(pokemonData.Shiny ? ":sparkles:SHINY:sparkles: " : "")}" +
                                         $"{richPokemonName} worth {pokemonData.BaseExperience} exp! {playerData.Pokeballs}/{pokeballMax} Poké Balls remaining.";
                        Embed[] embeds = new Embed[]
                        {
                            new EmbedBuilder()
                            .WithImageUrl(pokemonData.ImageUrl)
                            .Build()
                        };
                        if (newBadgeMessages != null)
                        {
                            string newMessage = String.Join("\n", newBadgeMessages);
                            newMessage += "\n" + message;
                            message = newMessage;
                        }
                        // Send Discord reply
                        await command.RespondAsync(message, embeds);
                    }
                    else
                    {
                        await command.RespondAsync("Error catching a Pokémon :( @arctycfox What's up?");
                        Console.WriteLine($"{username}'s command failed at " + DateTime.Now.ToString());
                    }
                }
                else // Not enough pokeballs
                {
                    await command.RespondAsync($"Sorry, you're out of Poké Balls for now. " +
                        $"The Poké Mart will automatically send you up to {pokeballMax} new Poké Balls every day. " +
                        $"Unfortunately, you will not receive a bonus Premier Ball.");
                }
            }

            // Pokescore command section
            if (command.CommandName == "pokescore")
            {
                if (playerData != null)
                {
                    string score = playerData.Experience.ToString("N0");
                    List<PokemonData> caughtPokemon = playerData.CaughtPokemon;
                    int catches = caughtPokemon.Count;

                    // Find best catch
                    if (playerData.CaughtPokemon.Any())
                    {
                        PokemonData bestPokemon = caughtPokemon.OrderByDescending(p => p.BaseExperience).FirstOrDefault();
                        int averageExp = playerData.Experience / playerData.CaughtPokemon.Count;
                        // Reply in Discord
                        string message = $"{username} has caught {catches} Pokémon totalling {score} exp. Average exp/catch: {averageExp}\n" +
                                         //$"Rank: \n" +
                                         $"They have earned {playerData.EarnedBadges.Count} out of {badges.Count} badges.\n" +
                                         $"Their best catch was this {(bestPokemon.Shiny ? "SHINY " : "")}" +
                                         $"{FixPokemonName(bestPokemon.Name)} worth {bestPokemon.BaseExperience} exp!";
                        Embed[] embeds = new Embed[]
                            {
                            new EmbedBuilder()
                            .WithImageUrl(bestPokemon.ImageUrl)
                            .Build()
                            };
                        await command.RespondAsync(message, embeds);
                    }
                    else
                    {
                        await command.RespondAsync($"{username} hasn't caught any Pokémon yet.");
                    }
                }
                else
                {
                    await command.RespondAsync($"No data for {username}.");
                }
            }

            // Leaderboard section
            if (command.CommandName == "pokeleaderboard")
            {
                // Log time til next pokeball in console - cheeky workaround to check it
                Console.WriteLine("Time until Pokeball reset: " + delay);

                // Get a sorted list of players
                var leaders = scoreboard.Values.ToList().OrderByDescending(p => p.Experience).ToList();
                // Add a message line for each of the top 10 from 10 to 1
                List<string> leaderMessages = new List<string>();
                int leaderCount = 10;
                if (leaders.Count < 10)
                {
                    leaderCount = leaders.Count;
                }
                for (int i = 0; i < leaderCount; i++)
                {
                    string leaderName = leaders[i].UserName;
                    string leaderExp = leaders[i].Experience.ToString("N0");
                    int averageExp = leaders[i].Experience / leaders[i].CaughtPokemon.Count;
                    string message = $"{i + 1}. {leaderName} - {leaderExp} exp. Average exp/catch: {averageExp}";
                    leaderMessages.Add(message);
                }
                // Output message to discord
                string leaderboardMessage = string.Join("\n", leaderMessages);
                await command.RespondAsync($"Top {leaderCount} trainers:\n" + leaderboardMessage);
            }

            // Badges section
            if (command.CommandName == "pokebadges")
            {
                string badgeCountMessage;

                //List<Badge> playerBadges = playerData.Badges.Keys.ToList();
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
                await command.RespondAsync(badgeCountMessage);
            }
        }

        public static string FixPokemonName(string pokemonName)
        {
            if (pokemonName.IndexOf('-') != -1)
            {
                int hyphenIndex = pokemonName.IndexOf('-');
                return pokemonName.Substring(0, hyphenIndex) + " " +
                       char.ToUpper(pokemonName[hyphenIndex + 1]) +
                       pokemonName.Substring(hyphenIndex + 2);
            }
            else
            {
                return pokemonName;
            }
        }

        private static async Task ResetPokeballs(object state)
        {
            // Create a temporary copy of scoreboard to avoid conflicts
            var playerDataList = scoreboard.Values.ToList();

            // Reset Pokeballs for each player in the copy
            foreach (var playerData in playerDataList)
            {
                playerData.Pokeballs = pokeballMax;
            }

            // Update the actual scoreboard atomically
            await Task.Run(() => scoreboard = new ConcurrentDictionary<ulong, PlayerData>(playerDataList.ToDictionary(p => p.UserId, p => p)));

            Console.WriteLine("Pokeballs have been reset for all players!");

            // Save the updated scoreboard
            await SaveScoreboardAsync();
        }

        private static ConcurrentDictionary<ulong, PlayerData> LoadScoreboard()
        {
            string filePath = "scoreboard.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Scoreboard data file not found. Creating a new one.");
                SaveScoreboardAsync();
                return new ConcurrentDictionary<ulong, PlayerData>();
            }
            else
            {
                Console.WriteLine($"Scoreboard loaded from {filePath}");
            }

            try
            {
                string jsonData = File.ReadAllText(filePath);
                ConcurrentDictionary<ulong, PlayerData> loadedScoreboard = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, PlayerData>>(jsonData);

                // Handle playerData version mismatch here
                foreach (var playerData in loadedScoreboard.Values)
                {
                    if (playerData.Version == 1)
                    {
                        // Upgrade to Version 2
                        playerData.Version = 2;
                        playerData.EarnedBadges = new List<Badge>();
                        //playerData.Badges = new Dictionary<Badge, DateTime>();
                        Console.WriteLine($"{playerData.UserName} upgraded playerData version from 1 to 2");
                    }
                }
                return loadedScoreboard;
            }
            catch (Exception ex)
            {
                // Handle deserialization errors or file access exceptions
                Console.WriteLine("Error loading scoreboard data: {0}", ex.Message);
                return new ConcurrentDictionary<ulong, PlayerData>();
            }
        }

        private static async Task SaveScoreboardAsync()
        {
            string filePath = "scoreboard.json";

            try
            {
                // Serialize the dictionary to JSON string
                string jsonData = JsonConvert.SerializeObject(scoreboard);

                // Write the JSON string to the file asynchronously
                await File.WriteAllTextAsync(filePath, jsonData);
                Console.WriteLine("Scoreboard data saved successfully.");
            }
            catch (Exception ex)
            {
                // Handle serialization errors or file access exceptions
                Console.WriteLine("Error saving scoreboard data: {0}", ex.Message);
            }
        }

        private static List<Badge> LoadBadges()
        {
            string filePath = "badges.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Badge data file not found.");
                return new List<Badge>();
            }
            else
            {
                try
                {
                    string jsonData = File.ReadAllText(filePath);
                    Console.WriteLine($"Badges loaded from {filePath}");
                    return JsonConvert.DeserializeObject<List<Badge>>(jsonData);
                }
                catch (Exception ex)
                {
                    // Handle deserialization errors or file access exceptions
                    Console.WriteLine("Error loading badge data: {0}", ex.Message);
                    return new List<Badge>();
                }
            }            
        }

        // Remove duplicate badges
        public static ConcurrentDictionary<ulong, PlayerData> RemoveDuplicateBadges(ConcurrentDictionary<ulong, PlayerData> scoreboard)
        {
            ConcurrentDictionary<ulong, PlayerData> newScoreboard = new ConcurrentDictionary<ulong, PlayerData>();

            foreach (var kvp in scoreboard)
            {
                ulong userId = kvp.Key;
                PlayerData playerData = kvp.Value;

                // Rebuild each playerData object
                PlayerData newPlayerData = new PlayerData
                {
                    Version = playerData.Version,
                    UserId = playerData.UserId,
                    UserName = playerData.UserName,
                    Experience = playerData.Experience,
                    Pokeballs = playerData.Pokeballs,
                    CaughtPokemon = playerData.CaughtPokemon,
                    EarnedBadges = new List<Badge>()              
                };

                HashSet<int> uniqueBadgeIds = new HashSet<int>();

                foreach (var badge in playerData.EarnedBadges)
                {
                    if (!uniqueBadgeIds.Contains(badge.Id))
                    {
                        // Badge is unique, add it to the new EarnedBadges list
                        newPlayerData.EarnedBadges.Add(badge);
                        uniqueBadgeIds.Add(badge.Id);
                    }
                }
                if(newScoreboard.TryAdd(userId, newPlayerData))
                {                    
                }
                else
                {
                    Console.WriteLine($"Unable to remove duplicate badges for {newPlayerData.UserName}. Data lost?");
                }
            }
            return newScoreboard;
        }

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            // TODO: Create a log file and log errors to it.
            return Task.CompletedTask;
        }
    }
}
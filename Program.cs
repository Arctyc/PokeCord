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
        const int maxPokemonId = 1025;
        const int shinyRatio = 256;
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;
        private static Timer _dailyResetTimer;

        //Cooldown data structure
        private static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        private static readonly TimeSpan _cooldownTime = TimeSpan.FromSeconds(300); // Set your desired cooldown time

        //Scoreboard data structure
        private static ConcurrentDictionary<ulong, PlayerData> scoreboard;

        private const int pokeballMax = 10;

        public static async Task Main(string[] args)
        {
            _client = new DiscordSocketClient();
            _services = ConfigureServices();
            _client.Log += Log;

            scoreboard = await LoadScoreboardAsync();

            // Calculate the time remaining until the next midnight
            TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            _dailyResetTimer = new Timer(async (e) => await ResetPokeballs(null), null, delay, TimeSpan.FromDays(1));
            Console.WriteLine("Time until Pokeball reset: " +  delay);
            // Run once on startup
            await ResetPokeballs(null);

            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.SlashCommandExecuted += SlashCommandHandler;

            _client.Ready += ClientReady;

            await Task.Delay(Timeout.Infinite);
        }

        public static async Task ClientReady()
        {
            await LoadScoreboardAsync();

            var catchCommand = new SlashCommandBuilder()
                .WithName("catch")
                .WithDescription("Catch a Pokémon!");

            var scoreCommand = new SlashCommandBuilder()
                .WithName("pokescore")
                .WithDescription("View your PokeCord score.");
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(catchCommand.Build());
                Console.WriteLine("Creating catch command...");
                await _client.CreateGlobalApplicationCommandAsync(scoreCommand.Build());
                Console.WriteLine("Creating score command...");
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
            string username = command.User.Username;
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
                    CaughtPokemon = new List<PokemonData>()
                };
                if (scoreboard.TryAdd(userId, playerData))
                {
                    originalPlayerData = playerData;
                    Console.WriteLine($"New PlayerData for {username} added with userId {userId}");
                }
                else
                {
                    Console.WriteLine($"Unable to add PlayerData for {username} added with userId {userId}");
                }
            }

            // Catch command section
            if (command.CommandName == "catch")
            {
                // Check cooldown information
                if (_lastCommandUsage.TryGetValue(userId, out DateTime lastUsed))
                {
                    Console.WriteLine($"{username} dict entry read: key {username} value {lastUsed}");
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
                if (!_lastCommandUsage.TryAdd(userId, DateTime.UtcNow)) // If unable to add new cooldown for user
                {
                    //Cooldown exists so update existing cooldown
                    if (_lastCommandUsage.TryUpdate(userId, DateTime.UtcNow, lastUsed))
                    {
                        Console.WriteLine($"Cooldown updated for {username}");
                    }
                    else
                    {
                        Console.WriteLine($"Unable to update dict for {username} with data {userId}:{DateTime.UtcNow}");
                    }
                }
                Console.WriteLine($"{username} dict entry update attempted: key {username} value {DateTime.UtcNow}");

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
                        playerData.Experience += (int)pokemonData.BaseExperience; //BUG: Occasionally null reference error!?
                        playerData.CaughtPokemon.Add(pokemonData);
                        playerData.Pokeballs -= 1;

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

                        // Reply in Discord
                        string message = $"{username} caught a {(pokemonData.Shiny ? "SHINY " : "")}" +
                                         $"{pokemonData.Name} worth {pokemonData.BaseExperience} exp! {playerData.Pokeballs}/{pokeballMax} Poké Balls remaining.";                    
                        Embed[] embeds = new Embed[]
                        {
                            new EmbedBuilder()
                            .WithImageUrl(pokemonData.ImageUrl)
                            .Build()
                        };
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
                    await command.RespondAsync("Sorry, you're out of Poké Balls for today. " +
                        "The Poké Mart will automatically give you 10 new Poké Balls tomorrow! " +
                        "Unfortunately, you will not receive a bonus Premier Ball.");
                }
            }

            // Pokescore command section
            if (command.CommandName == "pokescore")
            {
                if (playerData != null)
                {
                    int score = playerData.Experience;
                    List<PokemonData> caughtPokemon = playerData.CaughtPokemon;
                    int catches = caughtPokemon.Count;

                    // Find best catch
                    if (playerData.CaughtPokemon.Any())
                    {
                        PokemonData bestPokemon = caughtPokemon.OrderByDescending(p => p.BaseExperience).FirstOrDefault();


                        // Reply in Discord
                        string message = $"{username} has caught {catches} Pokémon totalling {score} exp.\n" +
                                         $"Their best catch was a {(bestPokemon.Shiny ? "SHINY " : "")}" +
                                         $"{bestPokemon.Name} worth {bestPokemon.BaseExperience} exp!";
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

            // Save the updated scoreboard
            await SaveScoreboardAsync();

            Console.WriteLine("Pokeballs have been reset for all players!");
        }

        private static async Task<ConcurrentDictionary<ulong, PlayerData>> LoadScoreboardAsync()
        {
            // Path to your JSON file (replace with your actual path)
            string filePath = "scoreboard.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Scoreboard data file not found. Creating a new one.");
                await SaveScoreboardAsync();
                return new ConcurrentDictionary<ulong, PlayerData>();
            }
            else
            {
                Console.WriteLine($"Scoreboard loaded from {filePath}");
            }

            try
            {
                string jsonData = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, PlayerData>>(jsonData);
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
            // Path to your JSON file (replace with your actual path)
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

        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            // TODO: Create a log file and log errors to it.
            return Task.CompletedTask;
        }
    }
}
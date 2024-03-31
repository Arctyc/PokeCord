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
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;

        //Cooldown data structure
        private static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        private static readonly TimeSpan _cooldownTime = TimeSpan.FromSeconds(300); // Set your desired cooldown time

        //Scoreboard data structure
        private static readonly ConcurrentDictionary<ulong, PlayerData> scoreboard = LoadScoreboard();



        public static async Task Main(string[] args)
        {
            _client = new DiscordSocketClient();
            _services = ConfigureServices();
            _client.Log += Log;

            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.SlashCommandExecuted += SlashCommandHandler;

            _client.Ready += ClientReady;

            await Task.Delay(Timeout.Infinite);
        }

        public static async Task ClientReady()
        {            
            LoadScoreboard();

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

            if (command.CommandName == "catch")
            {
                if (_lastCommandUsage.TryGetValue(userId, out DateTime lastUsed))
                {
                    Console.WriteLine($"{username} dict entry read: key {username} value {lastUsed}");
                    TimeSpan elapsed = DateTime.UtcNow - lastUsed;
                    if (elapsed < _cooldownTime)
                    {
                        int timeRemaining = (int)_cooldownTime.TotalSeconds - (int)elapsed.TotalSeconds;
                        var cooldownUnixTime = (long)(DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        await command.RespondAsync($"Easy there, Ash Ketchum! I know you wanna Catch 'em all. You can catch another Pokémon <t:{cooldownUnixTime}:R>.");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Could not get value for last command usage by {username} with userID {userId}\n" +
                        $"New user, or dict read error.");
                }

                if (!_lastCommandUsage.TryAdd(userId, DateTime.UtcNow)) // If unable to add new cooldown for user
                {
                    //Cooldown exists so update existing cooldown
                    if (_lastCommandUsage.TryUpdate(userId, DateTime.UtcNow, lastUsed))
                    {
                    }
                    else
                    {
                        Console.WriteLine($"Unable to update dict for {username} with data {userId}:{DateTime.UtcNow}");
                    }
                }

                Console.WriteLine($"{username} dict entry update attempted: key {username} value {DateTime.UtcNow}");
                Console.WriteLine($"{username} used {command.Data.Name}");

                // Set up a new PokeSelector
                PokeSelector pokeSelector = new PokeSelector(maxPokemonId);
                // Set up PokeApiClient
                var pokeApiClient = _services.GetRequiredService<PokeApiClient>();
                // Get a new pokemon
                PokemonData pokemonData = await pokeSelector.GetRandomPokemon(pokeApiClient);

                if (pokemonData != null)
                {
                    // Update scoreboard
                    // Check for existing entry
                    if (scoreboard.TryGetValue(userId, out PlayerData playerData))
                    {
                        // Update existing player data
                        playerData.Experience += (int)pokemonData.BaseExperience;
                        playerData.CaughtPokemon.Add(pokemonData);
                    }
                    else
                    {
                        // Create new player data
                        PlayerData newPlayerData = new PlayerData
                        {
                            UserId = userId,
                            UserName = username,
                            Experience = (int)pokemonData.BaseExperience
                        };
                        newPlayerData.CaughtPokemon.Add(pokemonData);
                        scoreboard.TryAdd(userId, newPlayerData);
                    }

                    // Save the updated scoreboard data
                    SaveScoreboard();

                    // Reply in Discord
                    string message = $"{username} caught a {pokemonData.Name} worth {pokemonData.BaseExperience} exp!";
                    Embed[] embeds = new Embed[]
                    {
                    new EmbedBuilder()
                    .WithImageUrl(pokemonData.ImageUrl)
                    .Build()
                    };
                    await command.RespondAsync(message, embeds);
                    Console.WriteLine($"{username} caught a {pokemonData.Name}");
                }
                else
                {
                    await command.RespondAsync("Error catching a Pokémon :( @arctycfox What's up?");
                    Console.WriteLine($"{username}'s command failed at " + DateTime.Now.ToString());
                }
            }

            if (command.CommandName == "pokescore")
            {
                if (scoreboard.TryGetValue(userId, out PlayerData playerData))
                {
                    int score = playerData.Experience;
                    List<PokemonData> caughtPokemon = playerData.CaughtPokemon;
                    int catches = caughtPokemon.Count;

                    // Find best catch
                    PokemonData bestPokemon = caughtPokemon.OrderByDescending(p => p.BaseExperience).FirstOrDefault();

                    // Reply in Discord
                    string message = $"{username} has caught {catches} Pokémon totalling {score} exp.\n" +
                                     $"Their best catch was a {bestPokemon.Name} worth {bestPokemon.BaseExperience} exp!";
                    await command.RespondAsync(message);
                }
                else
                {
                    await command.RespondAsync($"{username} hasn't caught any Pokémon yet.");
                }
            }
        }

        private static ConcurrentDictionary<ulong, PlayerData> LoadScoreboard()
        {
            // Path to your JSON file (replace with your actual path)
            string filePath = "scoreboard.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Scoreboard data file not found. Creating a new one.");
                SaveScoreboard();
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

        private static void SaveScoreboard()
        {
            // Path to your JSON file (replace with your actual path)
            string filePath = "scoreboard.json";

            try
            {
                // Serialize the dictionary to JSON string
                string jsonData = JsonConvert.SerializeObject(scoreboard);

                // Write the JSON string to the file
                File.WriteAllText(filePath, jsonData);
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
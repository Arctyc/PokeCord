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

namespace PokeCord
{
    public class Program
    {
        const int maxPokemonId = 1025;
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;
        private static ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        private static readonly TimeSpan _cooldownTime = TimeSpan.FromSeconds(300); // Set your desired cooldown time


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

            var globalCommand = new SlashCommandBuilder()
                .WithName("catch")
                .WithDescription("Catch a Pokémon!");
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            }
            catch (HttpException ex)
            {
                Console.WriteLine("Could not create global command. " + ex.Message);
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

            if (!_lastCommandUsage.TryAdd(userId, DateTime.UtcNow))
            {
                if(_lastCommandUsage.TryUpdate(userId, DateTime.UtcNow, lastUsed))
                {

                }
                else
                {
                    Console.WriteLine($"Unable to update dict for {username} with data {userId}:{DateTime.UtcNow}");
                }
            }

            Console.WriteLine($"{username} dict entry update attempted: key {username} value {DateTime.UtcNow}");

            Console.WriteLine($"{username} used {command.Data.Name}");

            //var pokeSelector = _services.GetRequiredService<PokeSelector>();
            PokeSelector pokeSelector = new PokeSelector(maxPokemonId);

            var pokeApiClient = _services.GetRequiredService<PokeApiClient>();

            PokemonData pokemonData = await pokeSelector.GetRandomPokemon(pokeApiClient);

            if (pokemonData != null)
            {

                string message = $"{username} caught a {pokemonData.Name}!";
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
                await command.RespondAsync("Error catching a Pokémon :(");
                Console.WriteLine($"{username}'s command failed at " + DateTime.Now.ToString());
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
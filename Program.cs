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

namespace PokeCord
{
    public class Program
    {
        private static DiscordSocketClient _client;
        private static IServiceProvider _services;
        const int maxPokemonId = 1025;

        public static async Task Main(string[] args)
        {
            _client = new DiscordSocketClient();
            _services = ConfigureServices();
            _client.Log += Log;

            var token = await ReadToken();

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
            catch (ApplicationCommandException ex)
            {
                Console.WriteLine("Could not create global command");
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
            //var pokeSelector = _services.GetRequiredService<PokeSelector>();
            PokeSelector pokeSelector = new PokeSelector(maxPokemonId);

            var pokeApiClient = _services.GetRequiredService<PokeApiClient>();

            PokemonData pokemonData = await pokeSelector.GetRandomPokemon(pokeApiClient);

            if (pokemonData != null)
            {
                string message = $"You caught a {pokemonData.Name}!";
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
                await command.RespondAsync("Error catching a Pokémon :(");
            }
        }

        public static async Task<string> ReadToken()
        {
            string filePath = "PokeCordToken.txt";

            try
            {
                string token = await File.ReadAllTextAsync(filePath);
                if (token == null)
                {
                    throw new Exception($"Token is null, check token file at: {filePath}");
                }
                return token.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
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
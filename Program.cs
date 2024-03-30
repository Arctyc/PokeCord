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

        static async Task Main(string[] args)
        {
            _services = ConfigureServices();
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.Ready += ClientReady;           

            await MainASync();
        }

        private static async Task MainASync()
        {
            var token = await ReadToken();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Register the SlashCommandExecuted event handler
            _client.SlashCommandExecuted += async (command) => await HandleSlashCommandExecutedAsync(command, _client);

            await Task.Delay(Timeout.Infinite);
        }

        public static async Task ClientReady()
        {
            ulong guildId = 832504327045251082;

            var guild = _client.GetGuild(guildId);

            var guildCommands = new SlashCommandBuilder()
                .WithName("catch")
                .WithDescription("Catch a Pokemon");

            try
            {
                await guild.CreateApplicationCommandAsync(guildCommands.Build());
            }
            catch(ApplicationCommandException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection()
                .AddSingleton<InteractionService>()
                .AddSingleton<DiscordSocketClient>()
                .AddScoped<InteractionHandler>()
                .AddScoped<InteractionModule>();

            return map.BuildServiceProvider();
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

        private static async Task HandleSlashCommandExecutedAsync(SocketSlashCommand command, DiscordSocketClient client)
        {
            using (var scope = _services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<InteractionHandler>();

                try
                {
                    await service.HandleInteraction(command);
                }
                catch (Exception ex)
                {
                    await command.RespondAsync("An error occurred while processing the command.");
                    Console.WriteLine("Error executing slash command");
                }
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
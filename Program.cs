using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Services;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace PokeCord
{
    public class Program
    {
        private static DiscordSocketClient _client = new DiscordSocketClient();
        private static InteractionService _interactionService;
        private static readonly InteractionServiceConfig _interactionServiceConfig = new InteractionServiceConfig();
        private static IServiceProvider _services { get; set; }
        private static IConfiguration _configuration;
        private static Timer _pokeballResetTimer;
        private static Timer _weeklyStartTimer;
        private static Timer _weeklyEndTimer;
        //Testing
        private static Timer _quickStartTimer;
        private static Timer _quickEndTimer;

        //Cooldown data structure
        public static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        public static readonly TimeSpan _standardCooldown = TimeSpan.FromSeconds(120); // Cooldown time in seconds
        public static readonly TimeSpan _xSpeedCooldown = TimeSpan.FromSeconds(60); // X Speed cooldown time in seconds
        public static readonly TimeSpan _testingCooldown = TimeSpan.FromSeconds(3); // Testing only!

        public static async Task Main()
        {
            // FETCH ENVIRONMENT VARIABLE TOKEN
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            //var token = Environment.GetEnvironmentVariable("DISCORD_TESTING_TOKEN");

            _client.Ready += ClientReady;
            _client.Log += LogAsync;

            // Login to Discord
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _configuration = new ConfigurationBuilder().Build();

            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
            // Keep bot running indefinitely
            await Task.Delay(Timeout.Infinite);
        }

        public static async Task ClientReady()
        {
            // Set up Discord.NET
            _services = ConfigureServices();
            _interactionService = new InteractionService(_client);
            _services.GetRequiredService<CommandHandler>();

            _client.InteractionCreated += async interaction =>
            {
                var context = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(context, _services);
            };

            var scoreboardService = _services.GetRequiredService<ScoreboardService>();
            await scoreboardService.LoadScoreboardAsync();
            await scoreboardService.LoadTeamScoreboardAsync();

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.RegisterCommandsGloballyAsync();
            Console.WriteLine("Commands registered in Program.cs.");

            // -- Daily Restock -- KEEP IN program.cs
            // Calculate the time remaining until the next pokeball restock
            TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            _pokeballResetTimer = new Timer(async (e) => await scoreboardService.RestockPokeballsAsync(null), null, delay, TimeSpan.FromDays(1));
            Console.WriteLine("Time until Pokeball reset: " + delay);

            // -- Weekly Reset --
            // Weekly Start Timer
            DateTime weeklyStartTime = DateTime.UtcNow.AddDays((DayOfWeek.Monday - DateTime.UtcNow.DayOfWeek) % 7);
            weeklyStartTime = weeklyStartTime.Date; // Set time to 00:00
            TimeSpan weeklyStartDelay = weeklyStartTime - DateTime.UtcNow;
            if (weeklyStartDelay < TimeSpan.Zero)
            {
                weeklyStartDelay = weeklyStartDelay.Add(TimeSpan.FromDays(7)); // Add 7 days
            }

            _weeklyStartTimer = new Timer(async (e) => await scoreboardService.StartWeeklyTeamsEventAsync(_client), null, weeklyStartDelay, TimeSpan.FromDays(7));
            Console.WriteLine("Time until Weekly Start Timer: " + weeklyStartDelay);

            // Weekly End Timer
            DateTime weeklyEndTime = DateTime.UtcNow.AddDays((DayOfWeek.Sunday - DateTime.UtcNow.DayOfWeek + 7) % 7);
            weeklyEndTime = weeklyEndTime.Date; // Set time to 00:00
            TimeSpan weeklyEndDelay = weeklyEndTime - DateTime.UtcNow;
            if (weeklyEndDelay < TimeSpan.Zero)
            {
                weeklyEndDelay = weeklyEndDelay.Add(TimeSpan.FromDays(7)); // Add 7 days
            }
            _weeklyEndTimer = new Timer(async (e) => await scoreboardService.EndWeeklyTeamsEventAsync(_client), null, weeklyEndDelay, TimeSpan.FromDays(7));
            Console.WriteLine("Time until Weekly End Timer: " + weeklyEndDelay);

            /*
            // Quick Championship Test Timer
            DateTime testStartTime = DateTime.UtcNow.AddSeconds(10);
            DateTime testEndTime = DateTime.UtcNow.AddSeconds(30);
            TimeSpan testStartDelay = testStartTime - DateTime.UtcNow;
            TimeSpan testEndDelay = testEndTime - DateTime.UtcNow;
            if (testStartDelay < TimeSpan.Zero) { testStartDelay = testStartDelay.Add(TimeSpan.FromSeconds(60));}
            if (testEndDelay < TimeSpan.Zero) { testEndDelay = testEndDelay.Add(TimeSpan.FromSeconds(60));}
            _quickStartTimer = new Timer(async (e) => await scoreboardService.StartWeeklyTeamsEventAsync(_client), null, testStartDelay, Timeout.InfiniteTimeSpan);
            _quickEndTimer = new Timer(async (e) => await scoreboardService.EndWeeklyTeamsEventAsync(_client), null, testEndDelay, TimeSpan.FromSeconds(30));
            Console.WriteLine("Time until Test Start Timer: " + testStartDelay);
            Console.WriteLine("Time until Test End Timer: " + testEndDelay);
            */

            //TODO - add command to give pokeballs to a specific user | set permissions for command in Discord
            // - /givepokeballs <user> <amount>
            /*
            var givepokeballsCommand = new SlashCommandBuilder()
            */
        }

        private static IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                //Discord.NET
                .AddSingleton(_configuration)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), _interactionServiceConfig))
                .AddSingleton<InteractionService>()
                //PokeCord
                .AddSingleton<CommandHandler>()
                .AddSingleton<PokeApiClient>()
                .AddTransient<ScoreboardService>()
                .AddTransient<BadgeService>()
                .AddSingleton<TeamAutocompleter>()
                //Build Collection
                .BuildServiceProvider();
        }

        public static IServiceProvider GetServices()
        {
            return _services;
        }

        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
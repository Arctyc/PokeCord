using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Services;
using System.Collections.Concurrent;
using System.Reflection;

namespace PokeCord
{
    public class Program
    {
        //const int maxPokemonId = 1025; // Highest Pokemon ID to be requested on PokeApi
        //const int shinyRatio = 256; // Chance of catching a shiny
        //private const int pokeballMax = 50; // Maximum catches per restock (currently hourly)
        //public const int teamCreateCost = 1000; // Cost in poke dollars to create a team
        //private const int pokemonDollarRatio = 10; // % to divide base exp by for awarding pokemon dollars 
        public const int teamCreateCost = 0; // Cost in poke dollars to create a team

        private static DiscordSocketClient _client = new DiscordSocketClient();
        private static InteractionService _interactionService;
        private static readonly InteractionServiceConfig _interactionServiceConfig = new InteractionServiceConfig();
        private static IServiceProvider _services { get; set; }
        private static IConfiguration _configuration;
        private static Timer _pokeballResetTimer;

        //Cooldown data structure
        public static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        public static readonly TimeSpan _cooldownTime = TimeSpan.FromSeconds(120); // Cooldown time in seconds

        public static async Task Main()
        {
            // FETCH ENVIRONMENT VARIABLE TOKEN
            //var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            var token = Environment.GetEnvironmentVariable("DISCORD_TESTING_TOKEN");

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

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.RegisterCommandsGloballyAsync();
            Console.WriteLine("Commands registered in Program.cs.");

            // -- Daily Restock -- KEEP IN program.cs
            // Calculate the time remaining until the next pokeball restock
            TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
            _pokeballResetTimer = new Timer(async (e) => await scoreboardService.ResetPokeballs(null), null, delay, TimeSpan.FromDays(1));
            Console.WriteLine("Time until Pokeball reset: " + delay);

            // Reset pokeballs when bot comes online
            // Unnecessary for now
            //await ResetPokeballs(null); 

            // Set up slash commands
            /*
            var viewTeamsCommand = new SlashCommandBuilder()
                .WithName("poketeams")
                .WithDescription("View all Poké Teams.");

            var createTeamCommand = new SlashCommandBuilder()
                .WithName("teamcreate")
                .WithDescription("Create a Poké Team!")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("team")
                    .WithDescription("Fill in the blank: Team ___.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
                );

            var joinTeamCommand = new SlashCommandBuilder()
                .WithName("teamjoin")
                .WithDescription("Join a Poké Team!")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("team")
                    .WithDescription("The name of the Poké Team you want to join.")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String)
                );
            */

            //TODO - add command to give pokeballs to a specific user | set permissions for command in Discord
            // - /givepokeballs <user> <amount>
            /*
            var givepokeballsCommand = new SlashCommandBuilder()
            */
            /*
            try
            {
                //FIX: Too many commands? "A ready handler is blocking the gateway task" -- Causes slow start
                //await _client.CreateGlobalApplicationCommandAsync(catchCommand.Build());
                //Console.WriteLine("Created command: catch");
                //await _client.CreateGlobalApplicationCommandAsync(scoreCommand.Build());
                //Console.WriteLine("Created command: pokescore");
                //await _client.CreateGlobalApplicationCommandAsync(leaderboardCommand.Build());
                //Console.WriteLine("Created command: pokeleaderboard");
                //await _client.CreateGlobalApplicationCommandAsync(badgesCommand.Build());
                //Console.WriteLine("Created command: pokebadges");
                //await _client.CreateGlobalApplicationCommandAsync(viewTeamsCommand.Build());
                //Console.WriteLine("Created command: poketeams");
                //await _client.CreateGlobalApplicationCommandAsync(createTeamCommand.Build());
                //Console.WriteLine("Created command: teamcreate");
                //await _client.CreateGlobalApplicationCommandAsync(joinTeamCommand.Build());
                //Console.WriteLine("Created command: teamjoin");
            }
            catch (HttpException ex)
            {
                Console.WriteLine("Could not create a command. " + ex.Message);
            }
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
                //Build Collection
                .BuildServiceProvider();
        }

        /*
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

            // Catch command section
            /*
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
                        playerData.Experience += (int)pokemonData.BaseExperience; // Award experience points                        
                        playerData.Pokeballs -= 1; // subtract one pokeball from user's inventory
                        playerData.PokemonDollars += (int)pokemonData.BaseExperience / pokemonDollarRatio; // award pokemon dollars
                        playerData.CaughtPokemon.Add(pokemonData); // Add the pokemon to the player's list of caught pokemon

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
                                playerData.Pokeballs += badge.BonusPokeballs;

                                string newBadgeMessage = $"{username} has acquired the {badge.Name}! +{badge.BonusPokeballs} Poké Balls!\n" +
                                                         $"{badge.Description}\n";
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
                        string richPokemonName = CleanOutput.FixPokemonName(pokemonData.Name);
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
                    TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
                    int timeRemaining = (int)delay.TotalSeconds;
                    var cooldownUnixTime = (long)(DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    await command.RespondAsync($"Sorry, you're out of Poké Balls for now. " +
                        $"The Poké Mart will automatically send you up to {pokeballMax} new Poké Balls <t:{cooldownUnixTime}:R>. " +
                        $"Unfortunately, you will not receive a bonus Premier Ball.");
                }
            }
            */
        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
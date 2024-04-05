using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Data;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;

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
        private static IServiceProvider _services;
        private static IConfiguration _configuration;
        private static Timer _pokeballResetTimer;

        //TODO: Weekly leaderboard

        //Cooldown data structure
        public static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = new ConcurrentDictionary<ulong, DateTime>();
        public static readonly TimeSpan _cooldownTime = TimeSpan.FromSeconds(120); // Cooldown time in seconds

        /* // Moved to Data
        // Individual scoreboard data structure
        private static ConcurrentDictionary<ulong, PlayerData> scoreboard = new ConcurrentDictionary<ulong, PlayerData>();
        // Team scoreboard data structure
        private static List<Team> teamScoreboard = new List<Team>();
        */

        //TODO: Create a timer to batch save to file every so often

        private static readonly InteractionServiceConfig _interactionServiceConfig = new InteractionServiceConfig();

        public static async Task Main(string[] args)
        {
            // FETCH ENVIRONMENT VARIABLE TOKEN
            //var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            var token = Environment.GetEnvironmentVariable("DISCORD_TESTING_TOKEN");

            _client.Ready += ClientReady;
            _client.Log += LogAsync;

            // Login to Discord
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "DC_")
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

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
            _interactionService = new InteractionService(_client);
            _services = ConfigureServices();

            await _services.GetRequiredService<CommandHandler>()
            .InitializeAsync();

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

            // Reset pokeballs when bot comes online
            // Unnecessary for now
            //await ResetPokeballs(null); 

            // Set up slash commands
            /* Moved to SlashCommands
            var catchCommand = new SlashCommandBuilder()
                .WithName("catch")
                .WithDescription("Catch a Pokémon!");
            */

            /*
            var scoreCommand = new SlashCommandBuilder()
                .WithName("pokescore")
                .WithDescription("View your PokeCord score.");
            */

            /*
            var leaderboardCommand = new SlashCommandBuilder()
                .WithName("pokeleaderboard")
                .WithDescription("Show a list of the trainers with the most exp.");
            */

            /* -- Double check these added
            var badgesCommand = new SlashCommandBuilder()
                .WithName("pokebadges")
                .WithDescription("Show a list of your earned badges.");

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
                .AddSingleton(_configuration)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), _interactionServiceConfig))
                .AddSingleton<CommandHandler>()
                .AddSingleton<PokeApiClient>()
                .AddSingleton<InteractionService>()
                .AddTransient<ScoreboardService>()
                .AddTransient<BadgeService>()
                .BuildServiceProvider();
            /*
            var services = new ServiceCollection();
            services.AddSingleton<PokeApiClient>(); // Add PokeApiClient as Singleton
            return services.BuildServiceProvider(); // Build and return the service provider
            */
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

        /*
        // Pokescore command section
        if (command.CommandName == "pokescore")
        {
            if (playerData != null)
            {
                string score = playerData.Experience.ToString("N0");
                string pokemonDollars = playerData.PokemonDollars.ToString("N0");
                List<PokemonData> caughtPokemon = playerData.CaughtPokemon;
                int catches = caughtPokemon.Count;

                // Find best catch
                if (playerData.CaughtPokemon.Any())
                {
                    PokemonData bestPokemon = caughtPokemon.OrderByDescending(p => p.BaseExperience).FirstOrDefault();

                    int averageExp = playerData.Experience / playerData.CaughtPokemon.Count;

                    // Format Discord reply
                    string message = $"{username} has caught {catches} Pokémon totalling {score} exp.\n" +
                                     //$"Rank: \n" +
                                     $"Average exp/catch: {averageExp}\n" +
                                     $"Pokémon Dollars: {pokemonDollars}" +
                                     $"They have earned {playerData.EarnedBadges.Count} out of {badges.Count} badges.\n" +
                                     $"Their best catch was this {(bestPokemon.Shiny ? "SHINY " : "")}" +
                                     $"{CleanOutput.FixPokemonName(bestPokemon.Name)} worth {bestPokemon.BaseExperience} exp!";

                    Embed[] embeds = new Embed[]
                        {
                        new EmbedBuilder()
                        .WithImageUrl(bestPokemon.ImageUrl)
                        .Build()
                        };
                    // Reply in Discord
                    await command.RespondAsync(message, embeds);
                }
                else
                {
                    // Reply in Discord
                    await command.RespondAsync($"{username} hasn't caught any Pokémon yet.");
                }
            }
            else
            {
                // Reply in Discord
                await command.RespondAsync($"No data for {username}.");
            }
        }
        */

        /*
        // Leaderboard section
        if (command.CommandName == "pokeleaderboard")
        {
            TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
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
        */

        /*
        // Badges section
        if (command.CommandName == "pokebadges")
        {
            string badgeCountMessage;

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

            // Reply in Discord
            await command.RespondAsync(badgeCountMessage);
        }
        */

        // Teams section

        /*
        // View teams
        if (command.CommandName == "poketeams")
        {
            TeamManager teamManager = new TeamManager();
            string message = TeamManager.ViewTeams(command, teamScoreboard);

            // Reply in Discord
            await command.RespondAsync(message);
        }
        */

        /*
        // Create team
        if (command.CommandName == "teamcreate")
        {
            // Pass to CreateTeam method for 
            (string message, Team team) = TeamManager.CreateTeam(command, playerData, teamCreateCost, teamScoreboard);
            if (team.Id == -1)
            {
                // Do not charge
                await command.RespondAsync(message);
            }
            teamScoreboard.Add(team);
            await SaveTeamScoreboardAsync();
            playerData.PokemonDollars -= teamCreateCost;
            // Reply in Discord
            await command.RespondAsync(message);
        }
        */

        /*
        // Join team
        if (command.CommandName == "teamjoin")
        {
            (bool joined, string message) = TeamManager.JoinTeam(command);
            if (!joined)
            {
                await command.RespondAsync(message);
            }
            string? teamName = command.Data.Options.First().Value.ToString();
            // Add player to team
            Team teamToJoin = teamScoreboard.Find(t => t.Name == teamName);
            teamToJoin.Players.Add(playerData);
            await SaveTeamScoreboardAsync();
            // Reply in Discord
            await command.RespondAsync(message);
        }

    }
    */

        /*
        public static List<Team> GetTeamList()
        {
            List<Team> teams = new List<Team>();
            return teams;
        }
        */

        /* // Moved to own class
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
        */


        /* // Moved to scoreboard service
        private static async Task ResetPokeballs(object state)
        {
            // Create a temporary copy of scoreboard to avoid conflicts
            var playerDataList = scoreboard.Values.ToList();

            // Reset Pokeballs for each player in the copy
            foreach (var playerData in playerDataList)
            {
                if (playerData.Pokeballs < pokeballMax)
                {
                    playerData.Pokeballs = pokeballMax;
                }                
            }

            // Update the actual scoreboard atomically
            await Task.Run(() => scoreboard = new ConcurrentDictionary<ulong, PlayerData>(playerDataList.ToDictionary(p => p.UserId, p => p)));

            Console.WriteLine("Pokeballs have been reset for all players!");

            // Save the updated scoreboard
            await SaveScoreboardAsync();
        }
        */

        /*
        private static List<Team> LoadTeamScoreboard()
        {
            string filePath = "teamscoreboard.json";
            List<Team> loadedTeamScoreboard = new List<Team>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Team scoreboard data file not found. Creating a new one.");
                SaveTeamScoreboardAsync();
                return new List<Team>();
            }
            else
            {
                Console.WriteLine($"Team scoreboard loaded from {filePath}");
            }

            try
            {
                string jsonData = File.ReadAllText(filePath);
                loadedTeamScoreboard = JsonConvert.DeserializeObject<List<Team>>(jsonData);

                // Handle Team.cs version mismatch
                // Version 1 => 2
                //foreach (var team in teamScoreboard)
                //{
                //}

                return loadedTeamScoreboard;
            }
            catch (Exception ex)
            {
                // Handle deserialization errors or file access exceptions
                Console.WriteLine("Error loading scoreboard data: {0}", ex.Message);
                return new List<Team>();
            }
        }

        private static async Task SaveTeamScoreboardAsync()
        {
            string filePath = "teamscoreboard.json";

            try
            {
                // Serialize the team scoreboard to JSON string
                string jsonData = JsonConvert.SerializeObject(teamScoreboard);

                // Write the JSON string to the file asynchronously
                await File.WriteAllTextAsync(filePath, jsonData);
                Console.WriteLine("Team scoreboard data saved successfully.");
            }
            catch (Exception ex)
            {
                // Handle serialization errors or file access exceptions
                Console.WriteLine("Error saving team scoreboard data: {0}", ex.Message);
            }
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

                // Handle playerData version mismatch
                foreach (var playerData in loadedScoreboard.Values)
                {
                    // Version 1=>2
                    if (playerData.Version == 1)
                    {
                        // Upgrade to Version 2
                        playerData.Version = 2;
                        playerData.EarnedBadges = new List<Badge>();
                        //playerData.Badges = new Dictionary<Badge, DateTime>();
                        Console.WriteLine($"{playerData.UserName} upgraded playerData version from 1 to 2");
                    }
                    // Version 2=>3
                    if (playerData.Version == 2)
                    {
                        playerData.Version = 3;
                        playerData.PokemonDollars = 100; // Give 100 free pokemon dollars to everyone upon upgrade
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
                // Serialize the scoreboard to JSON string
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
        */

        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
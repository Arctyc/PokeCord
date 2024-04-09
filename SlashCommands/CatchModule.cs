using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Collections.Concurrent;


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

        //Cooldown data structure
        public static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = Program._lastCommandUsage;
        public static readonly TimeSpan _cooldownTime = Program._cooldownTime; // Cooldown time in seconds

        public CatchModule(IServiceProvider services)
        {
            Console.Write("Loaded command: catch\n");
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
            PlayerData originalPlayerData = new PlayerData();
            List<Badge> badges = _badgeService.GetBadges();
            Console.WriteLine($"{username} used catch");

            // Get the PlayerData instance from the scoreboard
            PlayerData playerData = new PlayerData();

            if (_scoreboard.TryGetPlayerData(userId, out originalPlayerData))
            {
                // PlayerData exists for this userId
                playerData = originalPlayerData;
                Console.WriteLine($"PlayerData found for {username} {userId}");
            }
            else
            {
                // PlayerData does not exist for this userId, create new
                // TODO: Update this with new player data each version. Current version: 3
                playerData = new PlayerData
                {
                    UserId = userId,
                    UserName = username,
                    Experience = 0,
                    WeeklyExperience = 0,
                    Pokeballs = ScoreboardService.pokeballRestockAmount,
                    CaughtPokemon = new List<PokemonData>(),
                    WeeklyCaughtPokemon = new List<PokemonData>(),
                    EarnedBadges = new List<Badge>(),
                    TeamId = -1                    
            };
                if (_scoreboard.TryAddPlayerData(userId, playerData))
                {
                    originalPlayerData = playerData;
                    Console.WriteLine($"New PlayerData for {username} added with userId {userId}");
                }
                else
                {
                    Console.WriteLine($"Unable to add PlayerData for {username} with userId {userId}");
                    await RespondAsync($"Something went wrong setting up your player profile.");
                }
            }

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
                    await RespondAsync($"Easy there, Ash Ketchum! I know you Gotta Catch 'Em All, " +
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
                // Get a new pokemon
                PokemonData pokemonData = await pokeSelector.GetRandomPokemon(_pokeApiClient);

                if (pokemonData != null)
                {
                    Console.WriteLine($"{username} caught a {(pokemonData.Shiny ? "shiny " : "")}{pokemonData.Name} #{pokemonData.PokedexId}");
                    int pokemonDollarValue = (int)pokemonData.BaseExperience / pokemonDollarRatio;

                    // Update the existing playerData instance
                    playerData.Experience += (int)pokemonData.BaseExperience;// Award overall experience points
                    playerData.WeeklyExperience += (int)pokemonData.BaseExperience;// Award weekly experience points
                    playerData.Pokeballs -= 1; // subtract one pokeball from user's inventory
                    playerData.PokemonDollars += pokemonDollarValue; // award pokemon dollars
                    if (playerData.PokemonDollars > currencyCap) { playerData.PokemonDollars = currencyCap; } // Cap player pokemondollars
                    playerData.CaughtPokemon.Add(pokemonData); // Add the pokemon to the player's list of caught pokemon
                    playerData.WeeklyCaughtPokemon.Add(pokemonData); // Add the pokemon to the player's weekly list of caught pokemon

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
                    string message = $"{(onTeam ? "Team " : "")}{playerTeam} {username} caught {(startsWithVowel ? "an" : "a")} {(pokemonData.Shiny ? ":sparkles:SHINY:sparkles: " : "")}" +
                                     $"{richPokemonName} worth {pokemonData.BaseExperience} exp and {pokemonDollarValue} Pokémon Dollars!\n" +
                                     $"{playerData.Pokeballs} Poké Balls remaining.";
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
                    await RespondAsync(message, embeds);
                }
                else
                {
                    await RespondAsync("Error catching a Pokémon :( @arctycfox What's up?");
                    Console.WriteLine($"{username}'s command failed at " + DateTime.Now.ToString());
                }
            }
            else // Not enough pokeballs
            {
                TimeSpan delay = TimeSpan.FromHours(24) - DateTime.Now.TimeOfDay;
                int timeRemaining = (int)delay.TotalSeconds;
                var cooldownUnixTime = (long)(DateTime.UtcNow.AddSeconds(timeRemaining).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                await RespondAsync($"Sorry, you're out of Poké Balls for now. " +
                    $"The Poké Mart will automatically send you up to {ScoreboardService.pokeballRestockAmount} new Poké Balls <t:{cooldownUnixTime}:R>. " +
                    $"Unfortunately, you will not receive a bonus Premier Ball.");
            }
        }
    }
}

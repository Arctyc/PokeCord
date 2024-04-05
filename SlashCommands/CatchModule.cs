using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokeCord.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using PokeCord.Helpers;
using Discord.WebSocket;


namespace PokeCord.SlashCommands
{
    public class CatchModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Discord.NET services
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        // PokeCord services
        private readonly CommandHandler _handler;
        private readonly ScoreboardService _scoreboard;
        private readonly BadgeService _badgeService;
        private readonly PokeApiClient _pokeApiClient;

        // TODO: These should probably be in a config file
        const int maxPokemonId = 1025; // Highest Pokemon ID to be requested on PokeApi
        const int shinyRatio = 256; // Chance of catching a shiny
        private const int pokemonDollarRatio = 10; // % to divide base exp by for awarding pokemon dollars

        //Cooldown data structure
        public static readonly ConcurrentDictionary<ulong, DateTime> _lastCommandUsage = Program._lastCommandUsage;
        public static readonly TimeSpan _cooldownTime = Program._cooldownTime; // Cooldown time in seconds

        public CatchModule(IServiceProvider services)
        {
            // Discord.NET services
            _client = services.GetRequiredService<DiscordSocketClient>();
            _interactionService = services.GetRequiredService<InteractionService>();
            //PokeCord services
            _handler = services.GetRequiredService<CommandHandler>();
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
                playerData = new PlayerData
                {
                    UserId = userId,
                    UserName = username,
                    Experience = 0,
                    Pokeballs = ScoreboardService.pokeballMax,
                    CaughtPokemon = new List<PokemonData>(),
                    EarnedBadges = new List<Badge>()
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

                    // Format Discord Reply
                    string richPokemonName = CleanOutput.FixPokemonName(pokemonData.Name);
                    bool startsWithVowel = "aeiouAEIOU".Contains(richPokemonName[0]);
                    if (pokemonData.Shiny) { startsWithVowel = false; }
                    string message = $"{username} caught {(startsWithVowel ? "an" : "a")} {(pokemonData.Shiny ? ":sparkles:SHINY:sparkles: " : "")}" +
                                     $"{richPokemonName} worth {pokemonData.BaseExperience} exp! {playerData.Pokeballs}/{ScoreboardService.pokeballMax} Poké Balls remaining.";
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
                    $"The Poké Mart will automatically send you up to {ScoreboardService.pokeballMax} new Poké Balls <t:{cooldownUnixTime}:R>. " +
                    $"Unfortunately, you will not receive a bonus Premier Ball.");
            }
        }
    }
}

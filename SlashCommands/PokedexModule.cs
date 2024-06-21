using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;
using System.Text.Json; // For testing

namespace PokeCord.SlashCommands
{
    public class PokedexModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlayerDataService _playerDataService;

        public PokedexModule(IServiceProvider services)
        {
            Console.Write("Loaded command: pokedex\n");
            _playerDataService = services.GetRequiredService<PlayerDataService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("pokedex", "View your Pokédex!")]
        public async Task PokedexCommand()
        {
            // Get player data
            ulong userId = Context.User.Id;            
            PlayerData playerData = await _playerDataService.TryGetPlayerDataAsync(userId);
            // Get pokedex list
            Pokedex pokedex = new Pokedex();
            List<PokemonData> userPokedex = await pokedex.GetUserPokedexAsync(playerData);
            Console.WriteLine($"{playerData.UserName} has {userPokedex.Count} of 1025 Pokémon");

            // Json output for testing
            /*
            string fileName = $"{playerData.UserName}_PokedexTest.json";
            string jsonString = JsonSerializer.Serialize(userPokedex);
            File.WriteAllText(fileName, jsonString);
            Console.WriteLine($"Player Pokedex saved as {fileName}");
            */

            await RespondAsync($"{playerData.UserName} has caught {userPokedex.Count}/1025 Pokémon!");
        }
    }
}

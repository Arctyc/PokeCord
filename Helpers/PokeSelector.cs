using PokeApiNet;
using PokeCord.Data;
using System.Security.Cryptography;

namespace PokeCord.Helpers
{
    public class PokeSelector
    {
        // Highest and lowest Pokemon ID for special Pokemon
        private readonly int _minPokemonIdSpecial = 10001;
        private readonly int _maxPokemonIdSpecial = 10277;

        // Highest Pokemon ID to be requested on PokeApi
        private readonly int _maxPokemonId = 1025;

        // Chance of catching a shiny
        private readonly int _shinyRatio = 256;

        // Chance of catching a shiny with shiny charm item
        private readonly int _charmShinyRatio = 128;

        // Amount to multiply base exp by if shiny
        private const int shinyExpMultiplier = 4;

        // Default experience to be used in the case that there is no base exp provided
        private const int defaultExperience = 150;

        public PokeSelector()
        {
        }

        // Get a random Pokemon
        public async Task<PokemonData> GetRandomPokemon(PokeApiClient pokeApiClient, PlayerData playerData)
        {
            int playerShinyRatio = GetPlayerShinyRatio(playerData);
            int randomId = GetRandomId(1, _maxPokemonId + 1);
            return await GetPokemon(pokeApiClient, randomId, playerShinyRatio);
        }

        // Get an event Pokemon
        public async Task<PokemonData> GetEventPokemon(PokeApiClient pokeApiClient, PlayerData playerData)
        {
            int playerShinyRatio = GetPlayerShinyRatio(playerData);
            int randomId = GetRandomId(_minPokemonIdSpecial, _maxPokemonIdSpecial + 1);
            return await GetPokemon(pokeApiClient, randomId, playerShinyRatio);
        }

        // Get the shiny ratio for a player
        private int GetPlayerShinyRatio(PlayerData playerData)
        {
            return CheckPlayerShinyCharm(playerData) ? _charmShinyRatio : _shinyRatio;
        }

        // Check if a player has a shiny charm
        private bool CheckPlayerShinyCharm(PlayerData playerData)
        {
            return playerData.PokeMartItems.TryGetValue("Shiny Charm", out _);
        }

        // Get a random Pokemon ID
        private int GetRandomId(int min, int max)
        {
            return RandomNumberGenerator.GetInt32(min, max);
        }

        // Get a Pokemon
        private async Task<PokemonData> GetPokemon(PokeApiClient pokeApiClient, int pokemonId, int shinyRatio)
        {
            int shinyCheck = RandomNumberGenerator.GetInt32(1, shinyRatio + 1);
            bool shiny = shinyCheck == shinyRatio;
            Console.WriteLine($"PokeSelector Values - PokemonID: {pokemonId}, Shiny Roll: {shinyCheck}/{shinyRatio}");

            Pokemon pokemon = await pokeApiClient.GetResourceAsync<Pokemon>(pokemonId);

            if (pokemon != null)
            {
                // Assign experience and avoid null with default.
                int experience = pokemon.BaseExperience ?? defaultExperience;
                pokemon.Name = char.ToUpper(pokemon.Name[0]) + pokemon.Name.Substring(1);

                string? imageUrl;
                if (!shiny)
                {
                    // Assign default image
                    imageUrl = pokemon.Sprites.Other.OfficialArtwork.FrontDefault;
                }
                else
                {
                    // Assign shiny image
                    imageUrl = pokemon.Sprites.Other.OfficialArtwork.FrontShiny;
                    experience *= shinyExpMultiplier; // Add shiny exp
                }
                // Default image
                if (imageUrl == null)
                {
                    imageUrl = "https://imgur.com/M66swhe";
                }
                return new PokemonData
                {
                    PokedexId = pokemon.Id,
                    Name = pokemon.Name,
                    ImageUrl = imageUrl,
                    BaseExperience = experience,
                    Timestamp = DateTime.UtcNow,
                    Shiny = shiny
                };
            }
            else
            {
                Console.WriteLine($"Error fetching data for Pokemon ID: {pokemonId}");
                return null;
            }
        }
    }

    // Simple data structure for Pokemon information
    public class PokemonData
    {
        public int PokedexId { get; set; }
        public bool Shiny { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int? BaseExperience { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class PokeSelector
    {
        private readonly Random _random;
        private readonly int _maxPokemonId = 1025;

        public PokeSelector(int maxPokemonId)
        {
            _random = new Random();
            _maxPokemonId = maxPokemonId;
        }

        public async Task<PokemonData> GetRandomPokemon(PokeApiClient pokeApiClient)
        {
            int randomId = _random.Next(1, _maxPokemonId + 1); // Generate random ID within range

            /*
             * TODO:
             * 1: Check for a local cache
             * 2: If there is one, check if randomId is part of it
             * 3: If it is, use cache to display result
             * 4: If not, use rest of method
             */


            Pokemon pokemon = await pokeApiClient.GetResourceAsync<Pokemon>(randomId);

            if (pokemon != null)
            {
                string imageUrl = pokemon.Sprites.Other.OfficialArtwork.FrontDefault;
                if (pokemon.BaseExperience == null) { pokemon.BaseExperience = 50; } // Avoid null experience
                return new PokemonData { Name = pokemon.Name, ImageUrl = imageUrl, BaseExperience = (int)pokemon.BaseExperience, Timestamp = DateTime.UtcNow };
            }
            else
            {
                Console.WriteLine($"Error fetching data for Pokemon ID: {randomId}");
                return null;
            }
        }
    }

    public class PokemonData // Simple data structure for Pokemon information
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int BaseExperience { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

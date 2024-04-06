﻿using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.Helpers
{
    public class PokeSelector
    {
        private readonly Random _random;

        //TODO: Collect a range of alternate forms (10001-10277) that have artwork, give them a chance to be caught
        /*
        private readonly int _minPokemonIdStandard = 1;
        private readonly int _maxPokemonIdStandard = 1025;

        private readonly double _upperRangeProbability = 0.05; // 5% chance of selecting alternate form
        */
        private readonly int _maxPokemonId = 1025; // Highest Pokemon ID to be requested on PokeApi
        private readonly int _shinyRatio = 256; // Chance of catching a shiny
        private int defaultExperience = new Random().Next(75, 126); // exp to be used in the case that there is no base exp provided

        public PokeSelector(int maxPokemonId, int shinyRatio)
        {
            _random = new Random();
            _maxPokemonId = maxPokemonId;
            _shinyRatio = shinyRatio;
        }

        public async Task<PokemonData> GetRandomPokemon(PokeApiClient pokeApiClient)
        {
            int randomId = _random.Next(1, _maxPokemonId + 1); // Generate random ID within range
            int shinyCheck = _random.Next(1, _shinyRatio + 1); // Check for a shiny catch
            bool shiny = false;
            if (shinyCheck == _shinyRatio)
            {
                shiny = true;
            }

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
                    experience *= 4;
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
                Console.WriteLine($"Error fetching data for Pokemon ID: {randomId}");
                return null;
            }
        }
    }

    public class PokemonData // Simple data structure for Pokemon information
    {
        public int PokedexId { get; set; }
        public bool Shiny { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int? BaseExperience { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
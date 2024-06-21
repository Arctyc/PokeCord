using PokeCord.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.Helpers
{
    public class Pokedex
    {
        public Pokedex()
        {
        }

        public async Task<List<PokemonData>> GetUserPokedexAsync(PlayerData playerData)
        {
            // Regular and shiny
            /*
            return await Task.Run(() =>
            {
                return playerData.CaughtPokemon
                    .Where(p => p.PokedexId < 10001) // Do not include special/Alternate forms (Those will be somewhere else)
                    .GroupBy(p => new { p.PokedexId, p.Shiny }) // Group entries which by either unique pokemon or unqiue shiny
                    .Select(g => g.First()) // Select unique entries
                    .OrderBy(p => p.PokedexId)
                    .ToList();
            });
            */

            // Shiny in place of regular
            return await Task.Run(() =>
            {
                var uniquePokemon = playerData.CaughtPokemon
                    .Where(p => p.PokedexId < 10001)
                    .GroupBy(p => p.PokedexId)
                    .Select(g =>
                    {
                        // Check if there's a shiny Pokemon in the group
                        var shinyPokemon = g.FirstOrDefault(p => p.Shiny);
                        return shinyPokemon ?? g.First(); // Return shiny if exists, otherwise the first one
                    })
                    .OrderBy(p => p.PokedexId)
                    .ToList();

                return uniquePokemon;
            });
        }
    }
}

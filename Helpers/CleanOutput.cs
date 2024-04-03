using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.Helpers
{
    public class CleanOutput
    {
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
    }
}

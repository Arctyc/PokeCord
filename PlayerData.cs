using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    internal class PlayerData
    {
        public ulong UserId {  get; set; }
        public string UserName { get; set; }
        public int Experience { get; set; }
        public List<PokemonData> CaughtPokemon {  get; set; }
        public PlayerData()
        {
            CaughtPokemon = new List<PokemonData>();
        }
    }
}

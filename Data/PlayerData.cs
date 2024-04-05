using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokeCord.Helpers;

namespace PokeCord.Data
{
    public class PlayerData
    {
        public int Version { get; set; } = 3;
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public int Experience { get; set; }
        public int Pokeballs { get; set; }
        public int PokemonDollars { get; set; } // New!
        public List<PokemonData> CaughtPokemon { get; set; }
        public List<Badge> EarnedBadges { get; set; } // Fixedish version

        public PlayerData()
        {
            CaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
            //Badges = new Dictionary<Badge, DateTime>();
        }
    }
}

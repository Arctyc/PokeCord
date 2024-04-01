using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class PlayerData
    {
        public int Version { get; set; } = 2;
        public ulong UserId {  get; set; }
        public string UserName { get; set; }
        public int Experience { get; set; }
        public int Pokeballs { get; set; }
        public List<PokemonData> CaughtPokemon {  get; set; }
        public List<Badge> EarnedBadges { get; set; } // Fixedish version

        // Broken
        //public Dictionary<Badge, DateTime> Badges { get; set; } = new Dictionary<Badge, DateTime>();
        public PlayerData()
        {
            CaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
            //Badges = new Dictionary<Badge, DateTime>();
        }
    }
}

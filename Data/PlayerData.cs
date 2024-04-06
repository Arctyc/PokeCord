using PokeCord.Helpers;

namespace PokeCord.Data
{
    public class PlayerData
    {
        public int Version { get; set; } = 3;
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public int Experience { get; set; }
        public int WeeklyExperience { get; set; } // New!
        public int Pokeballs { get; set; }
        public int PokemonDollars { get; set; } // New!
        public List<PokemonData> CaughtPokemon { get; set; }
        public List<Badge> EarnedBadges { get; set; }

        public PlayerData()
        {
            CaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
        }
    }
}

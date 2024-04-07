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
        public int TeamId { get; set; } = -1; // New!
        public int Pokeballs { get; set; }
        public int PokemonDollars { get; set; } // New!
        public List<PokemonData> CaughtPokemon { get; set; }
        public List<Badge> EarnedBadges { get; set; }

        public PlayerData()
        {
            CaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
        }
        public PlayerData(ulong userId, string userName, int experience, int weeklyExperience, int pokeballs, int pokemonDollars)
        {
            UserId = userId;
            UserName = userName;
            Experience = experience;
            WeeklyExperience = weeklyExperience;
            Pokeballs = pokeballs;
            PokemonDollars = pokemonDollars;
            CaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
        }
    }
}

using PokeCord.Helpers;

namespace PokeCord.Data
{
    public class PlayerData
    {
        public int Version { get; set; } = 4; // Update when changes are made to class structure
        public ulong _id { get; set; } // "_id" is primary key in MongoDB, set to Discord User ID.
        public string UserName { get; set; } // Discord user display name
        public int Experience { get; set; } // Cumulative experience from pokemon
        public int WeeklyExperience { get; set; } // Only this week's experience for use in weekly championship/leaderboard
        public int TeamId { get; set; } = -1; // Default -1 for no team
        public int Pokeballs { get; set; }
        public int PokemonDollars { get; set; }
        public List<PokemonData> CaughtPokemon { get; set; }
        public List<PokemonData> WeeklyCaughtPokemon { get; set; }
        public List<Badge> EarnedBadges { get; set; }
        public Dictionary<string, int> PokeMartItems { get; set; }

        public PlayerData()
        {
            UserName = string.Empty;
            CaughtPokemon = new List<PokemonData>();
            WeeklyCaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
            PokeMartItems = new Dictionary<string, int>();
        }
        public PlayerData(ulong userId, string userName, int experience, int weeklyExperience, int pokeballs, int pokemonDollars)
        {
            _id = userId;
            UserName = userName;
            Experience = experience;
            WeeklyExperience = weeklyExperience;
            Pokeballs = pokeballs;
            PokemonDollars = pokemonDollars;
            CaughtPokemon = new List<PokemonData>();
            WeeklyCaughtPokemon = new List<PokemonData>();
            EarnedBadges = new List<Badge>();
            PokeMartItems = new Dictionary<string, int>();
        }
    }
}

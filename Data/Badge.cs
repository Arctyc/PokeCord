using PokeCord.Helpers;

namespace PokeCord.Data
{
    public class Badge
    {
        public int Version { get; set; } = 2;
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageAddress { get; set; }
        public int BonusPokeballs { get; set; }
        public List<PokemonData>? GymPokemon { get; set; }
    }

    public class BadgeManager
    {
        public List<Badge> UpdateBadgesAsync(PlayerData playerData, List<Badge> badges, PokemonData pokemonData)
        {
            List<Badge> newBadges = new List<Badge>();
            List<Badge> allBadges = new List<Badge>(badges);

            // Ignore badges already acquired by player
            foreach (Badge badge in playerData.EarnedBadges)
            {
                if (allBadges.Any(b => b.Id == badge.Id))
                {
                    // Player has already earned this badge, do not check for it
                    allBadges.RemoveAll(b => b.Id == badge.Id);
                }
            }
            // Check for new acquisitions
            foreach (Badge badge in allBadges)
            {
                // Handle non-pokemon specfic badges
                if (badge.GymPokemon == null)
                {
                    badge.GymPokemon = new List<PokemonData>();
                }
                // Check whether player has caught all pokemon for this badge
                if (badge.GymPokemon.All(gymPokemon => playerData.CaughtPokemon.Any(
                                         ownPokemon => ownPokemon.PokedexId == gymPokemon.PokedexId))
                                         && badge.Id != 2)
                {
                    // Add the badge to the player's new badges
                    newBadges.Add(badge);
                }
                else
                {
                    Console.WriteLine($"{playerData.UserName} does not qualify for {badge.Name}");
                }
                // Check shiny badge
                if (badge.Id == 2 && (pokemonData.Shiny || playerData.CaughtPokemon.Any(p => p.Shiny)))
                {
                    newBadges.Add(badge);
                }

            }
            //pass data back
            return newBadges;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class Badge
    {
        public int Version { get; set; } = 1;
        public int Id { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Name { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Description { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public int BonusPokeballs { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public List<PokemonData> GymPokemon {  get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
            foreach(Badge badge in allBadges)
            {
                // Handle non-pokemon specfic badges
                if (badge.GymPokemon == null)
                {                    
                    badge.GymPokemon = new List<PokemonData>();
                }
                // Check whether player has caught all pokemon for this badge
                if (badge.GymPokemon.All(gymPokemon => playerData.CaughtPokemon.Contains(gymPokemon)) && badge.Id != 2)
                {
                    // Add the badge to the player's new badges
                    newBadges.Add(badge);
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

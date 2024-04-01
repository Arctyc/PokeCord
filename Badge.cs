using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class Badge
    {
        public int version = 1;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PokemonData> GymPokemon {  get; set; }
    }

    public class BadgeManager
    {
        public List<Badge> UpdateBadgesAsync(PlayerData playerData, List<Badge> allBadges)
        {
            List<Badge> newBadges = new List<Badge>();
            // Ignore badges already acquired by player
            foreach(Badge badge in playerData.Badges.Keys)
            {
                if (allBadges.Contains(badge))
                {
                    allBadges.Remove(badge);
                }
            }
            // Check for new acquisitions
            foreach(Badge badge in allBadges)
            {
                // Check whether player has caught all pokemon for this badge
                if (badge.GymPokemon.All(gymPokemon => playerData.CaughtPokemon.Contains(gymPokemon)))
                {
                    // Add the badge to the player's new badges
                    newBadges.Add(badge);
                }
            }            
            //pass data back
            return newBadges;
        }
    }
}

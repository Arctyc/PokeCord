using System.Text.Json;
using PokeCord.Data;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PokeCord.Services
{
    public class BadgeService
    {
        // Badge data structure
        private static List<Badge>? _badges;

        public BadgeService()
        {
        }

        public List<Badge> GetBadges()
        {
            _badges = LoadBadges();
            return _badges;
        }

        private static List<Badge> LoadBadges()
        {
            string filePath = "badges.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Badge data file not found.");
                return new List<Badge>();
            }
            else
            {
                try
                {
                    using var stream = File.OpenRead(filePath);
                    List<Badge> badges = JsonSerializer.Deserialize<List<Badge>>(stream) ?? new List<Badge>();

                    // Handle badge mismatch
                    foreach (Badge badge in badges)
                    {
                        // Version 1 => 2
                        badge.Version = 2;
                        badge.Id = badge.Id;
                        badge.Name = badge.Name;
                        badge.Description = badge.Description;
                        badge.ImageAddress = ""; // Get badge image address
                        badge.BonusPokeballs = badge.BonusPokeballs;
                        badge.GymPokemon = badge.GymPokemon;
                    }
                    return badges;
                }
                catch (Exception ex)
                {
                    // Handle deserialization errors or file access exceptions
                    Console.WriteLine("Error loading badge data: {0}", ex.Message);
                    return new List<Badge>();
                }
            }
        }

        // Remove duplicate badges
        /* - Probably unnecessary. Remove when sure not needed.
        public static ConcurrentDictionary<ulong, PlayerData> RemoveDuplicateBadges(ConcurrentDictionary<ulong, PlayerData> scoreboard)
        {
            Debug.Assert(scoreboard != null && scoreboard.Count > 0);

            foreach (var kvp in scoreboard)
            {
                ulong userId = kvp.Key;
                PlayerData playerData = kvp.Value;

                Debug.Assert(playerData != null && playerData.EarnedBadges != null);

                HashSet<Badge> uniqueBadges = new HashSet<Badge>();

                foreach (var badge in playerData.EarnedBadges)
                {
                    if (uniqueBadges.Add(badge))
                    {
                        // Badge is unique, do nothing
                        Console.WriteLine($"Added unique badge {badge.Name} to Earned Badges.");
                    }
                    else
                    {
                        Console.WriteLine($"{badge.Name} was not unqiue.");
                    }
                }

                playerData.EarnedBadges = uniqueBadges.ToList();
            }
            return scoreboard;
        }*/
    }
}

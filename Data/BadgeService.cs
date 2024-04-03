using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.Data
{
    public class BadgeService
    {
        // Badge data structure
        private static List<Badge> _badges;

        public BadgeService()
        {
            // Load badges
            _badges = LoadBadges();
        }

        public List<Badge> GetBadges()
        {
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
                    string jsonData = File.ReadAllText(filePath);
                    List<Badge> badges = JsonConvert.DeserializeObject<List<Badge>>(jsonData);

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

                    Console.WriteLine($"Badges loaded from {filePath}");
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
        public static ConcurrentDictionary<ulong, PlayerData> RemoveDuplicateBadges(ConcurrentDictionary<ulong, PlayerData> scoreboard)
        {
            ConcurrentDictionary<ulong, PlayerData> newScoreboard = new ConcurrentDictionary<ulong, PlayerData>();

            foreach (var kvp in scoreboard)
            {
                ulong userId = kvp.Key;
                PlayerData playerData = kvp.Value;

                // Rebuild each playerData object
                PlayerData newPlayerData = new PlayerData
                {
                    Version = playerData.Version,
                    UserId = playerData.UserId,
                    UserName = playerData.UserName,
                    Experience = playerData.Experience,
                    Pokeballs = playerData.Pokeballs,
                    CaughtPokemon = playerData.CaughtPokemon,
                    EarnedBadges = new List<Badge>()
                };

                HashSet<int> uniqueBadgeIds = new HashSet<int>();

                foreach (var badge in playerData.EarnedBadges)
                {
                    if (!uniqueBadgeIds.Contains(badge.Id))
                    {
                        // Badge is unique, add it to the new EarnedBadges list
                        newPlayerData.EarnedBadges.Add(badge);
                        uniqueBadgeIds.Add(badge.Id);
                    }
                }
                if (newScoreboard.TryAdd(userId, newPlayerData))
                {
                }
                else
                {
                    Console.WriteLine($"Unable to remove duplicate badges for {newPlayerData.UserName}. Data lost?");
                }
            }
            return newScoreboard;
        }

    }
}

using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.Events
{
    internal class EventMysteryEgg
    {
        private readonly IServiceProvider _services = Program.GetServices();
        private readonly ScoreboardService _scoreboard;

        public const string itemKey = "MysteryEgg";
        public const int itemCharges = 3;
        public EventMysteryEgg()
        {
            _scoreboard = _services.GetRequiredService<ScoreboardService>();
        }

        public (bool, string) CheckEgg(PlayerData playerData)
        {
            // Check if player already has an egg
            // Return true if the egg is hatched on this catch
            bool hasEgg = playerData.PokeMartItems.TryGetValue(itemKey, out int EggProgress);
            string message = string.Empty;
            if (hasEgg)
            {
                playerData.PokeMartItems[itemKey] -= 1;
                EggProgress -= 1;
                switch (EggProgress)
                {
                    case 2:
                        message = "Catch 2 more Pokémon to hatch your Mystery Egg :egg:!";
                        return (false, message);
                    case 1:
                        message = "Catch 1 more Pokémon to hatch your Mystery Egg :egg:!";
                        return (false, message);
                    case 0:
                        message = RemoveEgg(playerData);
                        return (true, message);
                    default:
                        return (false, message);
                }
            }
            else
            {
                message = AddEgg(playerData);
                return (false, message);
            }
        }

        public string AddEgg(PlayerData playerData)
        {
            //TODO: Only add during event time
            playerData.PokeMartItems[itemKey] = itemCharges;
            string message = "You've received a Mystery Egg :egg:! Catch 3 more Pokémon to hatch it!";
            return message;
        }

        public string RemoveEgg(PlayerData playerData)
        {
            playerData.PokeMartItems.Remove(itemKey);
            string message = "You've hatched your Mystery Egg :egg:!";
            return message;
        }
    }
}

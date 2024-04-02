using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class Team
    {
        public int Version { get; set; } = 1;
        public int Id { get; set; }
        public string Name { get; set; }
        public List<PlayerData> Players { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Team()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Players = new List<PlayerData>();
        }
    }
}

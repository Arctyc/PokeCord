using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class TeamManager
    {
        public static string ViewTeams(SocketSlashCommand command)
        {
            List<Team> teams = Program.GetTeamList();                     
            
            // Formatted message (list of teams with score, ordered descending for output
            string message = "";

            throw new NotImplementedException();
            return message;
        }

        public static string CreateTeam(SocketSlashCommand command)
        {
            if (command.Data.Options.First().Value.ToString() == null)
            {
                command.RespondAsync("You must enter a team name to create a team");
            }
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string teamName = command.Data.Options.First().Value.ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            List<Team> teams = Program.GetTeamList();
            string message = "";

            throw new NotImplementedException();
            return message;
        }

        public static string JoinTeam(SocketSlashCommand command)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string teamName = command.Data.Options.First().Value.ToString();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            string message = "";

            throw new NotImplementedException();
            return message;
        }
    }
}

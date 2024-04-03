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
        public static string ViewTeams(SocketSlashCommand command, List<Team> teams)
        {
            // Formatted message (list of teams with score, ordered descending for output
            string message = "";

            if (teams.Count == 0)
            {
                message = $"There are no teams yet. Use /teamcreate to start one!";
            }
            foreach (Team team in teams)
            {
                // Sum experience of all players on team
                team.TeamExperience = team.Players.Sum(player => player.Experience);
            }            
            teams = teams.OrderByDescending(t => t.TeamExperience).ToList();
            for (int i = 0; i < teams.Count; i++)
            {
                string teamExp = teams[i].TeamExperience.ToString("N0");
                // Get list of name of each member of team
                List<String> members = new List<string>();
                foreach (PlayerData player in teams[i].Players)
                {
                    members.Add(player.UserName);
                }
                string membersList = string.Join(", ", members);
                message = $"{i + 1}. {teams[i].Name}: {teamExp} exp.\n" +
                          $"Trainers: {membersList}";
            }
            return message;
        }

        public static (string, Team) CreateTeam(SocketSlashCommand command, PlayerData playerData, int teamCreateCost, List<Team> teams)
        {
            string message;
            //List<Team> teams = Program.GetTeamList();
            if (teams == null)
            {
                teams = new List<Team>();
            }
            Team team = new Team();
            team.Id = -1; // If returned with this ID, currency will not be deducted from user
            string? teamName = command.Data.Options.First().Value.ToString();

            // Verify user can afford team creation cost
            if (playerData.PokemonDollars < teamCreateCost)
            {
                message = $"Sorry, you need {teamCreateCost} Pokémon Dollars to create a team. " +
                          $"You current have {playerData.PokemonDollars}";
            }
            // Check for existing name
            bool exists = false;
            foreach (Team existingTeam in teams)
            {
                if (teamName == existingTeam.Name)
                {
                    exists = true;
                }
            }
            if (exists)
            {
                command.RespondAsync($"There is already a team named Team {teamName}. Please choose something different.");
            }

            // Build team
            team.Id = teams.Count + 1;
            team.Name = teamName;
            team.TeamExperience = playerData.Experience;
            team.Players.Add(playerData);

            message = $"{command.User.GlobalName} has created Team {teamName}! If you'd like to join this team, type /teamjoin {teamName}.";
            return (message, team);
        }

        public static (bool, string) JoinTeam(SocketSlashCommand command)
        {
            string teamName = command.Data.Options.First().Value.ToString();
            string message = "";

            /*
            // Error handling
            if (unable to join team) 
            {
                // Player is already on a team
                // Team does not exist || Update command to get options from existing list of teams
                // Team is full?
                message = $"";
                return (false, message)
            }
            */

            throw new NotImplementedException();
            return (true, message);
        }
    }
}

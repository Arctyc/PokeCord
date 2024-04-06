using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Services;

namespace PokeCord.Helpers
{
    public class TeamManager
    {
        private readonly ScoreboardService scoreboardService;

        public TeamManager(IServiceProvider services)
        {
            scoreboardService = services.GetRequiredService<ScoreboardService>();
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
            // TODO: Error handling
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

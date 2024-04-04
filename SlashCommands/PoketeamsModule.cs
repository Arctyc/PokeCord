using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord.SlashCommands
{
    internal class PoketeamsModule : InteractionModuleBase<SocketInteractionContext>
    {
        /*
        [SlashCommand]
        public async Task PoketeamsCommand(InteractionContext context)
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
            // Reply in Discord
            await RespondAsync(message);
        }
        */
    }
}

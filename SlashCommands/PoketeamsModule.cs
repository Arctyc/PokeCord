using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    internal class PoketeamsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ScoreboardService scoreboardService;
        public PoketeamsModule(IServiceProvider services)
        {
            scoreboardService = services.GetRequiredService<ScoreboardService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("poketeams", "View all Poké Teams.")]
        public async Task PoketeamsCommand([Choice("View", "view"), Choice("Create", "create"), Choice("Join", "join")] string operation)
        {
            switch (operation)
            {
                case "view":
                    // Formatted message (list of teams with score, ordered descending for output
                    string viewMessage = "";

                    List<Team> teams = scoreboardService.GetTeams();

                    if (teams.Count == 0)
                    {
                        viewMessage = $"There are no teams yet. Use /teamcreate to start one!";
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
                        viewMessage = $"{i + 1}. {teams[i].Name}: {teamExp} exp.\n" +
                                  $"Trainers: {membersList}";
                    }
                    // Reply in Discord
                    await RespondAsync(viewMessage);
                    break;

                case "create":
                    string createMessage = "Not Implemented";

                    /*
                    // Pass to TeamManager
                    (string message, Team team) = TeamManager.CreateTeam(command, playerData, teamCreateCost, teamScoreboard);
                    if (team.Id == -1)
                    {
                        // Do not charge
                        await command.RespondAsync(message);
                    }
                    teamScoreboard.Add(team);
                    await SaveTeamScoreboardAsync();
                    playerData.PokemonDollars -= teamCreateCost;
                    // Reply in Discord
                    */

                    await RespondAsync(createMessage);
                    break;

                case "join":
                    string joinMessage = "Not Implemented";

                    /*
                    // Pass to TeamManager
                    (bool joined, string message) = TeamManager.JoinTeam(command);
                    if (!joined)
                    {
                        await command.RespondAsync(message);
                    }
                    string? teamName = command.Data.Options.First().Value.ToString();
                    // Add player to team
                    Team teamToJoin = teamScoreboard.Find(t => t.Name == teamName);
                    teamToJoin.Players.Add(playerData);
                    await SaveTeamScoreboardAsync();
                    // Reply in Discord
                    */

                    await RespondAsync(joinMessage);
                    break;

                default:
                    await RespondAsync($"No teams operation selected. Choose View, Create, or Join.");
                    break;
            }
        }
    }
}

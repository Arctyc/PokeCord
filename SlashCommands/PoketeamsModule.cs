using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    public class PoketeamsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlayerDataService _playerDataService;
        private readonly TeamChampionshipService _teamChampionshipService;
        public PoketeamsModule(IServiceProvider services)
        {
            Console.Write("Loaded command: poketeams\n");
            _playerDataService = services.GetRequiredService<PlayerDataService>();
            _teamChampionshipService = services.GetRequiredService<TeamChampionshipService>();
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("poketeams", "View all Poké Teams.")]
        public async Task PoketeamsCommand()
        {
            ulong userId = Context.User.Id;
            string username = Context.User.GlobalName;

            // Formatted message (list of teams with score, ordered descending for output
            string viewMessage = "";

            List<Team> teams = await _teamChampionshipService.GetTeamsAsync();
            if (teams.Count == 0)
            {
                viewMessage = $"There are no teams yet. Use /teamcreate to start one!";
            }
            else
            {
                for (int i = 0; i < teams.Count; i++)
                {
                    string teamExp = teams[i].TeamExperience.ToString("N0");

                    // Get list of name of each member of team
                    List<String> members = new List<string>();
                    foreach (ulong player in teams[i].Players)
                    {
                        PlayerData playerData = await _playerDataService.TryGetPlayerDataAsync(player);
                        if (playerData != null)
                        {
                            members.Add(playerData.UserName);
                        }
                    }
                    string membersList = string.Join(", ", members);
                    viewMessage += $"{i + 1}. Team {teams[i].Name}: {teamExp} exp.\n" +
                                  $"Trainers: {membersList}\n";
                }
            }
            
            // Reply in Discord
            await RespondAsync(viewMessage);
        }
    }
}
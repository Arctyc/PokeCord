using Discord;
using Discord.Interactions;
using PokeCord.Data;
using PokeCord.Helpers;
using PokeCord.Services;

namespace PokeCord.SlashCommands
{
    public class TeamJoinModule : InteractionModuleBase<SocketInteractionContext>
    {
        public TeamJoinModule(IServiceProvider services)
        {
            Console.Write("Loaded command: teamjoin\n");
        }

        [CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
        [IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
        [SlashCommand("teamjoin", "Join a Poké Team.")]        
        public async Task JoinTeamCommand(
            [Summary("team", "The Poké Team you would like to join.")]
            [Autocomplete(typeof(TeamAutocompleter))] string team)
        {
            await RespondAsync($"You are now a member of Team {team}");
        }
    }
}

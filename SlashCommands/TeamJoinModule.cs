using Discord;
using Discord.Interactions;
using PokeCord.Data;
using PokeCord.Helpers;

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
        public async Task JoinTeamCommand()
        {
            RespondAsync("Not implemented");
        }
    }
}

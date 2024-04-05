using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace PokeCord
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;

        public CommandHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services, IConfiguration config)
        {
            _client = client;
            _commands = commands;
            _services = services;
            _configuration = config;
            _client.Log += LogAsync;

            client.InteractionCreated += x =>
            {
                Task.Run(() => TryRunInteraction(x));
                return Task.CompletedTask;
            };

            // Subscribe to events
            _client.InteractionCreated += HandleInteraction;
            _commands.SlashCommandExecuted += HandleCommands;
            _commands.InteractionExecuted += HandleInteractionExecute;

            Console.WriteLine("Interactions setup in CommandHandler.cs");
        }

        public async Task InitializeAsync()
        {
            _client.Ready += ReadyAsync;
        }

        private async Task ReadyAsync()
        {
            Console.WriteLine("ReadyAsync called in CommandHandler.cs");
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                Console.WriteLine($"Received interaction: {interaction.Type}");
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _commands.ExecuteCommandAsync(context, _services);

                // Due to async nature of InteractionFramework, the result here may always be success.
                // That's why we also need to handle the InteractionExecuted event.
                if (!result.IsSuccess)
                    switch (result.Error)
                    {
                        case InteractionCommandError.UnmetPrecondition:
                            // implement
                            break;
                        default:
                            break;
                    }
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
        {
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task TryRunInteraction(SocketInteraction interaction)
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            var result = await _commands.ExecuteCommandAsync(ctx, _services).ConfigureAwait(false);
            Console.WriteLine($"Command executed:{result.IsSuccess}\nReason:{result.ErrorReason}");
            //Log.Information($"Button was executed:{result.IsSuccess}\nReason:{result.ErrorReason}");
        }

        private async Task HandleCommands(SlashCommandInfo command, Discord.IInteractionContext context, IResult result)
        {
            Console.WriteLine("HandleCommands called.");
            if (!result.IsSuccess)
            {
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // Handle unmet precondition errors
                        await context.Interaction.RespondAsync("You don't have permission to use this command.");
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // Handle unknown command errors
                        await context.Interaction.RespondAsync("That's not a valid command.");
                        break;
                    case InteractionCommandError.BadArgs:
                        // Handle bad argument errors
                        await context.Interaction.RespondAsync("You provided invalid arguments for the command.");
                        break;
                    case InteractionCommandError.Exception:
                        // Handle exceptions
                        await context.Interaction.RespondAsync("An unexpected error occurred while executing the command.");
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // Handle other errors
                        await context.Interaction.RespondAsync("The command was not executed successfully.");
                        break;
                    default:
                        // Handle other cases
                        await context.Interaction.RespondAsync("An unknown error occurred while executing the command.");
                        break;
                }
            }
            else
            {
                // Command execution was successful
                // You can add any additional logic here
            }
        }
        private static Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}

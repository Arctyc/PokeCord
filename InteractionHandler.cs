using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace PokeCord
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                var context = new SocketInteractionContext(_client, arg);
                await _commands.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

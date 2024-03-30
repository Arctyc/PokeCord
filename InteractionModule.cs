using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokeCord
{
    public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
    {

        [SlashCommand("catch", "Catch a Pokémon")]
        public async Task Catch()
        {
            string caller = "You";
            string pokemon = "Pokémon";
            await RespondAsync($"{caller} caught a {pokemon}");
        }
    }
}
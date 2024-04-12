using Discord;
using Discord.Interactions;

namespace PokeCord.Services
{
    public class PokemartAutocompleter : AutocompleteHandler
    {
        private readonly ScoreboardService scoreboardService;

        public PokemartAutocompleter()
        {

        }

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
        {
            IEnumerable<AutocompleteResult> PokemartItems = new[]
            {
                new AutocompleteResult("My Items", "My Items"),
                new AutocompleteResult("Menu","Menu"),
                new AutocompleteResult("Poké Balls","Poké Balls"),
                new AutocompleteResult("Amulet Coin","Amulet Coin"),
                new AutocompleteResult("Exp. Share","Exp. Share"),
                new AutocompleteResult("Lucky Egg","Lucky Egg"),
                new AutocompleteResult("Shiny Charm","Shiny Charm"),
                new AutocompleteResult("X Speed","X Speed")
            };
            return Task.FromResult(AutocompletionResult.FromSuccess(PokemartItems));
        }
    }
}
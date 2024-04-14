using Discord;
using Discord.Interactions;

namespace PokeCord.Services
{
    public class TeamAutocompleter : AutocompleteHandler
    {
        private readonly ScoreboardService scoreboardService;

        public TeamAutocompleter(ScoreboardService scoreboardService)
        {
            this.scoreboardService = scoreboardService;
        }

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction,
        IParameterInfo parameter, IServiceProvider services)
        {
            var teams = scoreboardService.GetTeams()
                .Select(x => x.Name)
                .Select(x => new AutocompleteResult(x, x));
            return Task.FromResult(AutocompletionResult.FromSuccess(teams));
        }
    }
}
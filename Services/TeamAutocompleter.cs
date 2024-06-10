using Discord;
using Discord.Interactions;
using PokeCord.Data;

namespace PokeCord.Services
{
    public class TeamAutocompleter : AutocompleteHandler
    {
        private readonly TeamChampionshipService _teamChampionService;

        public TeamAutocompleter(TeamChampionshipService teamChampionshipService)
        {
            _teamChampionService = teamChampionshipService;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction interaction,
            IParameterInfo parameter, IServiceProvider services)
        {
            // Await the Task to get the actual List<Team>
            List<Team> teams = await _teamChampionService.GetTeamsAsync();

            // Convert each team name to AutocompleteResult
            var suggestions = teams.Select(x => new AutocompleteResult(x.Name, x.Name));

            // Return the AutocompletionResult
            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}
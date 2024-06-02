using PokeCord.Helpers;

namespace PokeCord.Data
{
    internal class Scavenger
    {
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<PokemonData> ScavengerPokemon { get; set; }

    }
}

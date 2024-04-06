namespace PokeCord.Data
{
    public class Team
    {
        public int Version { get; set; } = 1;
        public int Id { get; set; }
        public string Name { get; set; }
        public int TeamExperience { get; set; }
        public List<PlayerData> Players { get; set; }

        public Team()

        {
            Players = new List<PlayerData>();
        }
    }
}

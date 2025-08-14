namespace ElectionApi.Models
{
    public class Country : Document
    {
        public string Name { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
        public List<string> Offices { get; set; } = new List<string>();
        public List<string> Candidates { get; set; } = new List<string>();
    }
}

namespace ElectionApi.Models
{
    public class Candidate : Document
    {
        public int OriginalId { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string? Part { get; set; }
        public string? Country { get; set; }
        public string? Parti { get; set; }
        public string? Image { get; set; }
    }
}

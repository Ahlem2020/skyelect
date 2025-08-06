namespace ElectionApi.Models
{
    public class Candidate : Document
    {
        public int OriginalId { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? Part { get; set; }
    }
}

namespace ElectionApi.Models
{
    public class Office : Document
    {
        public int OriginalId { get; set; }
        public int Offices { get; set; }
        public string? Code { get; set; }
        public long Registered { get; set; }
        public string? Province { get; set; }
        public string? CountryId { get; set; }
        public string? CountryName { get; set; }
    }
}

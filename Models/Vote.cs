namespace ElectionApi.Models
{
    public class Vote : Document
    {
        public int OfficeId { get; set; }
        public int CandidateId { get; set; }
        public long TotalVote { get; set; }
        public string? From {get;set;}
    }
}

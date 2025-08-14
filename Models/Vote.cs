namespace ElectionApi.Models
{
    public class Vote : Document
    {
        public string? Password { get; set; }
        public int Office { get; set; }
        public List<CandidateVote> Candidates { get; set; } = new List<CandidateVote>();
        public string? From { get; set; }

        // Legacy properties for backward compatibility
        public int OfficeId { get; set; }
        public int CandidateId { get; set; }
        public long TotalVote { get; set; }

        public class CandidateVote
        {
            public int Id { get; set; }
            public long? Vote { get; set; }
        }
    }
}

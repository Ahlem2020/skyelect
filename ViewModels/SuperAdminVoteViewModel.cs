using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class SuperAdminVoteViewModel
    {
        public string? Password { get; set; }
        
        [Required]
        public int Office { get; set; }
        
        public List<SuperAdminCandidateVoteViewModel> Candidates { get; set; } = new List<SuperAdminCandidateVoteViewModel>();
        
        public string? From { get; set; }
        
        // Legacy properties for backward compatibility
        public int OfficeId { get; set; }
        public int CandidateId { get; set; }
        public long TotalVote { get; set; }
    }

    public class SuperAdminCandidateVoteViewModel
    {
        [Required]
        public int Id { get; set; }
        public long? Vote { get; set; }
    }
}

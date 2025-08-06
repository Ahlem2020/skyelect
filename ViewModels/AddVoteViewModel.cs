using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class AddVoteViewModel
    {
        [Required]
        public string? Password { get; set; }
        [Required]
        public int Office { get; set; }
        [Required]
        [MinLength(1)]
        public List<CandidateVoteViewModel>? Candidates { get; set; }

        public class CandidateVoteViewModel
        {
            [Required]
            public int Id { get; set; }
            public long? Vote { get; set; }
        }
    }
}
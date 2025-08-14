using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class AddCandidateViewModel
    {
        public int OriginalId { get; set; }
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        public string Part { get; set; } = string.Empty;
        
        public string Country { get; set; } = string.Empty;
        
        public string Parti { get; set; } = string.Empty;
        
        public string Image { get; set; } = string.Empty;
    }

    public class UpdateCandidateViewModel
    {
        public int OriginalId { get; set; }
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        public string Part { get; set; } = string.Empty;
        
        public string Country { get; set; } = string.Empty;
        
        public string Parti { get; set; } = string.Empty;
        
        public string Image { get; set; } = string.Empty;
    }
}

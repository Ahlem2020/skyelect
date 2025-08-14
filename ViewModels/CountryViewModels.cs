using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class AddCountryViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Flag { get; set; } = string.Empty;
        
        public List<string> Offices { get; set; } = new List<string>();
        
        public List<string> Candidates { get; set; } = new List<string>();
    }

    public class UpdateCountryViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Flag { get; set; } = string.Empty;
        
        public List<string> Offices { get; set; } = new List<string>();
        
        public List<string> Candidates { get; set; } = new List<string>();
    }
}

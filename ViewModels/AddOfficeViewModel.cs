using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class AddOfficeViewModel
    {
        [Required]
        public int Id { get; set; }
        public int? Offices { get; set; }
        public string? Code { get; set; }
        [Required]
        public long Registered { get; set; }
        public string? Province { get; set; }
    }
}
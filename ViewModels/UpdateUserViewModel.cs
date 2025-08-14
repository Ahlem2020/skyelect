using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class UpdateUserViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public List<string> Roles { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;
    }
}

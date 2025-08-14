using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class TwoFactorLoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string TwoFactorCode { get; set; } = string.Empty;
    }

    public class AuthResponseViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public bool RequiresTwoFactor { get; set; } = false;
        public string? Message { get; set; }
    }

    public class Enable2FAViewModel
    {
        [Required]
        public bool Enable { get; set; }
    }

    public class Verify2FAViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateProfileViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        public string? CurrentPassword { get; set; }

        [MinLength(6)]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }
    }

    public class SetTwoFactorCodeViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
        
        [Range(1, 60)]
        public int ExpiryMinutes { get; set; } = 5;
    }
}

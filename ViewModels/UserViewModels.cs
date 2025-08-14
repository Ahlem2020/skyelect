using System.ComponentModel.DataAnnotations;

namespace ElectionApi.ViewModels
{
    public class UserProfileViewModel
    {
        public string? Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserViewModel
    {
        public string? Username { get; set; }
        
        [EmailAddress]
        public string? Email { get; set; }
        
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        
        // Password change fields
        public string? CurrentPassword { get; set; }
        
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string? NewPassword { get; set; }
    }

    public class TwoFactorStatusViewModel
    {
        public bool IsEnabled { get; set; }
        public bool HasSecret { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Generate2FASecretViewModel
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Enable2FACodeViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; } = string.Empty;
    }

    public class Disable2FAViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; } = string.Empty;
    }

    public class Verify2FACodeViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; } = string.Empty;
    }
}


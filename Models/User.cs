using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ElectionApi.Models
{
    public class User : Document
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        
        // 2FA Properties
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorCodeExpiry { get; set; }
        public bool TwoFactorCodeUsed { get; set; } = false;
        public string? TwoFactorSecret { get; set; } // For TOTP apps like Google Authenticator
    }
}

namespace ElectionApi.Settings
{
    public interface IJwtSettings
    {
        string Key { get; }
        string Issuer { get; }
        string Audience { get; }
        int ExpiresInHours { get; }
    }

    public class JwtSettings : IJwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiresInHours { get; set; }
    }
}

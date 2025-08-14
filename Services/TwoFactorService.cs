using ElectionApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace ElectionApi.Services
{
    public interface ITwoFactorService
    {
        Task<string> GenerateCodeAsync();
        Task<bool> SendCodeAsync(User user, string code);
        bool ValidateCodeFormat(string code);
        Task<bool> ValidateTOTPCodeAsync(string secret, string code);
        string GenerateSecretKey();
        Task<string> GenerateOTPCodeAsync(string secret);
    }

    public class TwoFactorService : ITwoFactorService
    {
        private readonly ILogger<TwoFactorService> _logger;
        private const int CodeLength = 6;
        private const int CodeValidityMinutes = 5;

        public TwoFactorService(ILogger<TwoFactorService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateCodeAsync()
        {
            // Generate a Facebook-like 6-digit code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString().PadLeft(CodeLength, '0');
            
            await Task.CompletedTask; // For async pattern
            return code;
        }

        public async Task<bool> SendCodeAsync(User user, string code)
        {
            try
            {
                // In a real application, implement SMS/Email sending here
                // For now, we'll just log the code for development purposes
                
                _logger.LogInformation($"2FA Code for user {user.Username}: {code}");
                
                // Example message similar to Facebook's format
                string message = $"Your Facebook code is {code}. This code will expire in {CodeValidityMinutes} minutes. Don't share this code with anyone.";
                _logger.LogInformation($"2FA message that would be sent: {message}");
                
                // Example implementations:
                // - Send SMS using Twilio, AWS SNS, etc.
                // - Send Email using SendGrid, AWS SES, etc.
                // - Send push notification
                
                // Simulate async operation
                await Task.Delay(100);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send 2FA code to user {user.Username}");
                return false;
            }
        }

        public bool ValidateCodeFormat(string code)
        {
            if (string.IsNullOrEmpty(code))
                return false;

            if (code.Length != CodeLength)
                return false;

            return code.All(char.IsDigit);
        }
        
        // Generate a random secret key for TOTP (Time-based One-Time Password)
        public string GenerateSecretKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; // Base32 characters
            var random = new Random();
            var key = new StringBuilder(32);
            
            for (int i = 0; i < 32; i++)
            {
                key.Append(chars[random.Next(chars.Length)]);
            }
            
            return key.ToString();
        }
        
        // Validate TOTP/OTP code for authenticator apps
        public async Task<bool> ValidateTOTPCodeAsync(string secret, string code)
        {
            try
            {
                _logger.LogInformation($"Validating code. Secret length: {secret?.Length ?? 0}, Code: {code}");
                
                // First, validate the code format
                if (!ValidateCodeFormat(code))
                {
                    _logger.LogWarning("Invalid code format - must be 6 digits");
                    return false;
                }
                
                // Check if the secret is provided
                if (string.IsNullOrEmpty(secret))
                {
                    _logger.LogWarning("Secret is null or empty");
                    return false;
                }
                
                // Try to parse the code to detect formatting patterns
                // For now, we'll assume 6-digit numeric code is standard
                if (!int.TryParse(code, out int codeInt))
                {
                    _logger.LogWarning("Failed to parse code as integer");
                    return false;
                }
                
                // Enhanced logging for better diagnostics
                _logger.LogInformation($"Beginning validation for code: {code} with secret starting with {(secret.Length > 4 ? secret.Substring(0, 4) : secret)}...");
                
                // Detect 2FA method based on the code format and secret
                // For this implementation, we'll try multiple methods and provide detailed logging
                
                // 1. Try as HOTP (counter-based) - this is event-based, not time-based
                _logger.LogInformation("Attempting HOTP (counter-based) validation...");
                for (long counter = 0; counter < 10; counter++)
                {
                    var generatedHotp = GenerateHOTPCode(secret, counter);
                    _logger.LogInformation($"  Testing HOTP counter {counter}: Generated code: {generatedHotp}");
                    
                    if (generatedHotp == code)
                    {
                        _logger.LogInformation($"HOTP code validation successful with counter {counter}");
                        // Here we could store the counter for this user if needed
                        return true;
                    }
                }
                
                // 2. Try as TOTP (time-based) with extended window for better compatibility
                _logger.LogInformation("Attempting TOTP (time-based) validation...");
                var currentCounter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
                // Use a wider window (±2 intervals) to account for clock skew
                for (int window = -2; window <= 2; window++)
                {
                    var counter = currentCounter + window;
                    var generatedTotp = GenerateHOTPCode(secret, counter);
                    
                    _logger.LogInformation($"  Testing TOTP window {window} (time offset: {window*30} seconds): Generated code: {generatedTotp}");
                    
                    if (generatedTotp == code)
                    {
                        _logger.LogInformation($"TOTP code validation successful with time window {window}");
                        return true;
                    }
                }
                
                // Detailed failure logging for diagnostics
                _logger.LogWarning($"Code validation failed - no matching code found for input: {code}");
                _logger.LogInformation($"Expected TOTP code for current time: {GenerateHOTPCode(secret, currentCounter)}");
                _logger.LogInformation($"Expected HOTP code for counter 0: {GenerateHOTPCode(secret, 0)}");
                
                await Task.CompletedTask; // For async pattern
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating code");
                return false;
            }
        }
        
        // Generate an OTP code based on a secret and the current time or counter
        public Task<string> GenerateOTPCodeAsync(string secret)
        {
            try
            {
                // For time-based codes (TOTP)
                var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
                var timeBasedCode = GenerateHOTPCode(secret, counter);
                
                // For event-based codes (HOTP)
                // We're starting with counter 0 for new setups
                var eventBasedCode = GenerateHOTPCode(secret, 0);
                
                // For this implementation, we'll use time-based (TOTP) by default
                // as it's more commonly used in authenticator apps
                _logger.LogInformation($"Generated TOTP code: {timeBasedCode}, HOTP code: {eventBasedCode}");
                
                return Task.FromResult(timeBasedCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP code");
                return Task.FromResult("000000"); // Return a default value on error
            }
        }
        
        // Generate HMAC-based One-Time Password code (works for both HOTP and TOTP)
        private string GenerateHOTPCode(string secret, long counter)
        {
            try
            {
                // Decode the Base32 secret key
                byte[] secretBytes = DecodeBase32(secret);
                
                // Create a counter byte array (8 bytes, big-endian)
                byte[] counterBytes = BitConverter.GetBytes(counter);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(counterBytes);
                }
                
                // Ensure we have 8 bytes for the counter (big-endian format)
                byte[] paddedCounter = new byte[8];
                // Copy the counter bytes to the end of the array
                Array.Copy(counterBytes, 0, paddedCounter, 8 - counterBytes.Length, counterBytes.Length);
                
                // Compute HMAC-SHA1 hash
                byte[] hash;
                using (HMACSHA1 hmac = new HMACSHA1(secretBytes))
                {
                    hash = hmac.ComputeHash(paddedCounter);
                }
                
                // Dynamic truncation to get 4 bytes
                int offset = hash[hash.Length - 1] & 0x0F;
                int binary = ((hash[offset] & 0x7F) << 24) |
                             ((hash[offset + 1] & 0xFF) << 16) |
                             ((hash[offset + 2] & 0xFF) << 8) |
                             (hash[offset + 3] & 0xFF);
                
                // Get 6 digits (modulo 10^6)
                int otp = binary % 1000000;
                
                // Format with leading zeros if needed
                return otp.ToString("D6");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HOTP code");
                return "000000"; // Return a default value on error
            }
        }
        
        // Base32 decoding (for converting the secret key to bytes)
        private byte[] DecodeBase32(string base32)
        {
            const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var bytes = new List<byte>();
            
            // Remove any padding characters
            base32 = base32.TrimEnd('=').ToUpper();
            
            // Process input 8 characters (40 bits) at a time
            for (int i = 0; i < base32.Length; i += 8)
            {
                long buffer = 0;
                int bitsLeft = 0;
                
                // Process up to 8 characters or until the end of the string
                int chars = Math.Min(8, base32.Length - i);
                for (int j = 0; j < chars; j++)
                {
                    buffer <<= 5;
                    int index = charset.IndexOf(base32[i + j]);
                    buffer |= (long)(index & 0x1F); // Mask to ensure we only use the lower 5 bits
                    bitsLeft += 5;
                    
                    if (bitsLeft >= 8)
                    {
                        bitsLeft -= 8;
                        bytes.Add((byte)(buffer >> bitsLeft));
                        buffer &= (1 << bitsLeft) - 1;
                    }
                }
            }
            
            return bytes.ToArray();
        }
    }
}

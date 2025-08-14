using ElectionApi.Models;
using ElectionApi.Repositories;
using BCrypt.Net;

namespace ElectionApi.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User> CreateUserAsync(string username, string email, string password, List<string> roles);
        Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
        string HashPassword(string password);
        Task UpdateLastLoginAsync(string userId);
        
        // 2FA Methods
        Task<string> GenerateTwoFactorCodeAsync(string userId);
        Task<bool> VerifyTwoFactorCodeAsync(string username, string code);
        Task<bool> Enable2FAAsync(string userId, bool enable);
        Task<User?> AuthenticateWithPasswordAsync(string username, string password);
        Task<User?> CompleteAuthenticationWith2FAAsync(string username, string code);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITwoFactorService _twoFactorService;

        public AuthService(IUserRepository userRepository, ITwoFactorService twoFactorService)
        {
            _userRepository = userRepository;
            _twoFactorService = twoFactorService;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            
            if (user == null || !user.IsActive)
                return null;

            if (!ValidatePassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User?> AuthenticateWithPasswordAsync(string username, string password)
        {
            var user = await AuthenticateAsync(username, password);
            if (user == null)
                return null;

            // If 2FA is enabled, generate and send code but don't complete authentication
            if (user.TwoFactorEnabled)
            {
                await GenerateTwoFactorCodeAsync(user.Id!);
                return user; // Return user but authentication is not complete
            }

            // If no 2FA, complete authentication
            await UpdateLastLoginAsync(user.Id!);
            return user;
        }

        public async Task<User?> CompleteAuthenticationWith2FAAsync(string username, string code)
        {
            Console.WriteLine($"Attempting to verify 2FA code for user '{username}'");
            
            var isValidCode = await VerifyTwoFactorCodeAsync(username, code);
            if (!isValidCode)
            {
                Console.WriteLine($"2FA verification failed for user '{username}'");
                return null;
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user != null)
            {
                Console.WriteLine($"2FA verification successful for user '{username}', updating login time and marking code as used");
                await UpdateLastLoginAsync(user.Id!);
                
                // Mark the 2FA code as used
                user.TwoFactorCodeUsed = true;
                await _userRepository.UpdateAsync(user);
            }
            else
            {
                Console.WriteLine($"User '{username}' not found after successful 2FA verification");
            }

            return user;
        }

        public async Task<string> GenerateTwoFactorCodeAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            // Generate a 6-digit code using the service
            var code = await _twoFactorService.GenerateCodeAsync();

            user.TwoFactorCode = code;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5); // Code expires in 5 minutes
            user.TwoFactorCodeUsed = false;

            await _userRepository.UpdateAsync(user);

            // Send the code via SMS/Email/etc.
            await _twoFactorService.SendCodeAsync(user, code);

            return code;
        }

        public async Task<bool> VerifyTwoFactorCodeAsync(string username, string code)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                Console.WriteLine($"2FA Verification failed: User '{username}' not found");
                return false;
            }

            Console.WriteLine($"Verifying 2FA code for user '{username}'. TwoFactorEnabled: {user.TwoFactorEnabled}");
            
            // Check if user has 2FA enabled
            if (!user.TwoFactorEnabled)
            {
                Console.WriteLine($"2FA Verification failed: 2FA not enabled for user '{username}'");
                return false;
            }
            
            // If user has a TOTP secret set, validate using OTP
            if (!string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                Console.WriteLine($"Using OTP validation for user '{username}' with secret: {user.TwoFactorSecret?.Substring(0, 5)}...");
                var isValidOtp = await _twoFactorService.ValidateTOTPCodeAsync(user.TwoFactorSecret ?? string.Empty, code);
                if (isValidOtp)
                {
                    Console.WriteLine($"OTP verification succeeded for user '{username}'");
                    return true;
                }
                else
                {
                    Console.WriteLine($"OTP verification failed for user '{username}' with code '{code}'");
                    return false;
                }
            }
            
            // Fall back to traditional 2FA code validation
            Console.WriteLine($"Falling back to traditional 2FA code validation for user '{username}'");
            
            // Check if code exists, hasn't expired, and hasn't been used
            if (string.IsNullOrEmpty(user.TwoFactorCode))
            {
                Console.WriteLine($"2FA Verification failed: No 2FA code exists for user '{username}'");
                return false;
            }
            
            if (user.TwoFactorCodeExpiry == null)
            {
                Console.WriteLine($"2FA Verification failed: No expiry time for 2FA code for user '{username}'");
                return false;
            }
            
            if (user.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                Console.WriteLine($"2FA Verification failed: Code expired at {user.TwoFactorCodeExpiry} for user '{username}'");
                return false;
            }
            
            if (user.TwoFactorCodeUsed)
            {
                Console.WriteLine($"2FA Verification failed: Code already used for user '{username}'");
                return false;
            }
            
            if (user.TwoFactorCode != code)
            {
                Console.WriteLine($"2FA Verification failed: Invalid code for user '{username}'. Expected '{user.TwoFactorCode}', received '{code}'");
                return false;
            }

            Console.WriteLine($"Traditional 2FA verification succeeded for user '{username}'");
            return true;
        }

        public async Task<bool> Enable2FAAsync(string userId, bool enable)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                    return false;

                user.TwoFactorEnabled = enable;
                
                // Clear any existing 2FA codes when disabling
                if (!enable)
                {
                    user.TwoFactorCode = null;
                    user.TwoFactorCodeExpiry = null;
                    user.TwoFactorCodeUsed = false;
                    user.TwoFactorSecret = null;
                }

                await _userRepository.UpdateAsync(user);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<User> CreateUserAsync(string username, string email, string password, List<string> roles)
        {
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Roles = roles,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TwoFactorEnabled = false
            };

            await _userRepository.CreateAsync(user);
            return user;
        }

        public async Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
        {
            return await Task.FromResult(ValidatePassword(password, hashedPassword));
        }

        private bool ValidatePassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public async Task UpdateLastLoginAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }
        }
    }
}

using ElectionApi.Models;
using ElectionApi.Services;
using ElectionApi.ViewModels;
using ElectionApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace ElectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseViewModel>> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _userRepository.UsernameExistsAsync(model.Username))
            {
                return BadRequest("Username already exists");
            }

            if (await _userRepository.EmailExistsAsync(model.Email))
            {
                return BadRequest("Email already exists");
            }

            try
            {
                var user = await _authService.CreateUserAsync(model.Username, model.Email, model.Password, model.Roles);
                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponseViewModel
                {
                    Token = token,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = user.Roles
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseViewModel>> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _authService.AuthenticateWithPasswordAsync(model.Username, model.Password);
            if (user == null)
            {
                return Unauthorized("Invalid username or password");
            }

            // If 2FA is enabled, don't return token yet
            if (user.TwoFactorEnabled)
            {
                Console.WriteLine($"2FA is enabled for user {user.Username}. Code: {user.TwoFactorCode}, Expiry: {user.TwoFactorCodeExpiry}");
                
                return Ok(new AuthResponseViewModel
                {
                    Token = string.Empty,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = user.Roles,
                    RequiresTwoFactor = true,
                    Message = "Please enter the 6-digit code sent to your registered method."
                });
            }

            // If no 2FA, return token immediately
            var token = _jwtService.GenerateToken(user);
            return Ok(new AuthResponseViewModel
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                RequiresTwoFactor = false
            });
        }

        [HttpPost("verify-2fa")]
        public async Task<ActionResult<AuthResponseViewModel>> VerifyTwoFactor(TwoFactorLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Add more detailed logging for troubleshooting
            Console.WriteLine($"2FA Verification attempt for user: {model.Username}, code length: {model.TwoFactorCode?.Length ?? 0}");
            
            // First check if the user exists
            var userExists = await _userRepository.GetByUsernameAsync(model.Username);
            if (userExists == null)
            {
                Console.WriteLine($"2FA Verification failed: User '{model.Username}' not found");
                return Unauthorized("User not found");
            }
            
            // Log the current state of the user's 2FA settings
            Console.WriteLine($"2FA settings for user {userExists.Username}:");
            Console.WriteLine($"  TwoFactorEnabled: {userExists.TwoFactorEnabled}");
            Console.WriteLine($"  TwoFactorCode: {userExists.TwoFactorCode}");
            Console.WriteLine($"  TwoFactorCodeExpiry: {userExists.TwoFactorCodeExpiry}");
            Console.WriteLine($"  TwoFactorCodeUsed: {userExists.TwoFactorCodeUsed}");
            Console.WriteLine($"  Current UTC time: {DateTime.UtcNow}");
            
            // Ensure code is not null (although it should be validated by ModelState)
            string code = model.TwoFactorCode ?? string.Empty;
            
            // Then attempt the 2FA verification
            var user = await _authService.CompleteAuthenticationWith2FAAsync(model.Username, code);
            if (user == null)
            {
                Console.WriteLine($"2FA verification failed, returning 401 Unauthorized");
                return Unauthorized("Invalid or expired 2FA code");
            }

            Console.WriteLine($"2FA verification successful, generating token for user {user.Username}");
            var token = _jwtService.GenerateToken(user);
            return Ok(new AuthResponseViewModel
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                RequiresTwoFactor = false,
                Message = "Authentication successful"
            });
        }

        [HttpPost("resend-2fa")]
        public async Task<IActionResult> ResendTwoFactorCode([FromBody] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || !user.TwoFactorEnabled)
            {
                return BadRequest("User not found or 2FA not enabled");
            }

            try
            {
                var code = await _authService.GenerateTwoFactorCodeAsync(user.Id!);
                
                // In a real application, you would send this via SMS/Email
                // For development, you might log it or return it
                return Ok(new { message = "2FA code resent successfully", code = code }); // Remove code from response in production
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating 2FA code: {ex.Message}");
            }
        }

        [HttpPost("enable-2fa")]
        public async Task<IActionResult> EnableTwoFactor(Enable2FAViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get current user from JWT token
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = await _authService.Enable2FAAsync(user.Id!, model.Enable);
            if (!result)
            {
                return BadRequest("Failed to update 2FA settings");
            }

            return Ok(new { message = $"2FA {(model.Enable ? "enabled" : "disabled")} successfully", twoFactorEnabled = model.Enable });
        }

        [HttpGet("2fa-status")]
        [Authorize]
        public async Task<IActionResult> GetTwoFactorStatus()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new { 
                username = user.Username,
                twoFactorEnabled = user.TwoFactorEnabled,
                hasActiveTwoFactorCode = !string.IsNullOrEmpty(user.TwoFactorCode) && 
                                       user.TwoFactorCodeExpiry > DateTime.UtcNow && 
                                       !user.TwoFactorCodeUsed
            });
        }

        [HttpGet("2fa/status")]
        [Authorize]
        public async Task<IActionResult> GetTwoFactorStatusV2()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new { 
                username = user.Username,
                email = user.Email,
                twoFactorEnabled = user.TwoFactorEnabled,
                hasSecret = !string.IsNullOrEmpty(user.TwoFactorSecret),
                hasActiveTwoFactorCode = !string.IsNullOrEmpty(user.TwoFactorCode) && 
                                       user.TwoFactorCodeExpiry > DateTime.UtcNow && 
                                       !user.TwoFactorCodeUsed
            });
        }

        [HttpPost("2fa/generate")]
        [Authorize]
        public async Task<IActionResult> GenerateTwoFactorSecret()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                // Get the TwoFactorService to generate a proper secret
                var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
                var secret = twoFactorService.GenerateSecretKey();
                
                user.TwoFactorSecret = secret;
                await _userRepository.UpdateAsync(user);

                // Generate a QR code URL for authenticator apps
                var appName = "ElectionApp";
                // Make it more user-friendly with a better label including the email
                var label = $"{appName}:{user.Email}";
                
                // Support both TOTP and HOTP formats
                // TOTP is more common for most authenticator apps
                var totpQrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(appName)}&algorithm=SHA1&digits=6&period=30";
                
                // HOTP requires a counter parameter
                var hotpQrCodeUrl = $"otpauth://hotp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(appName)}&algorithm=SHA1&digits=6&counter=0";
                
                // Use TOTP by default as it's more widely supported
                var qrCodeUrl = totpQrCodeUrl;
                
                // Use Google Chart API to generate QR code
                var googleChartUrl = $"https://chart.googleapis.com/chart?cht=qr&chs=300x300&chl={Uri.EscapeDataString(qrCodeUrl)}";

                // Log the QR code URL for debugging (remove in production)
                _logger.LogInformation($"Generated QR code URL: {qrCodeUrl}");
                _logger.LogInformation($"TOTP URL: {totpQrCodeUrl}");
                _logger.LogInformation($"HOTP URL: {hotpQrCodeUrl}");

                // Generate a current OTP code to verify implementation is working
                var currentCode = await twoFactorService.GenerateOTPCodeAsync(secret);
                _logger.LogInformation($"Current OTP code for testing: {currentCode}");

                return Ok(new { 
                    secret = secret,
                    qrCode = googleChartUrl,
                    manualEntryKey = secret,
                    totpUri = totpQrCodeUrl,
                    hotpUri = hotpQrCodeUrl,
                    message = "2FA secret generated successfully. Scan the QR code with your authenticator app or manually enter the secret key."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating 2FA secret: {ex.Message}");
            }
        }

        [HttpPost("2fa/enable")]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactorAuth([FromBody] Verify2FAViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { 
                    message = "Invalid data provided", 
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) 
                });
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return BadRequest(new { 
                    message = "2FA secret not generated. Please generate a secret first.",
                    action = "generate_secret"
                });
            }

            // Verify the code using enhanced validation that detects the 2FA method
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            bool isCodeValid = await twoFactorService.ValidateTOTPCodeAsync(user.TwoFactorSecret ?? string.Empty, model.Code);
            
            if (!isCodeValid)
            {
                _logger.LogWarning($"Invalid verification code provided by user {username}. Code: {model.Code}");
                return BadRequest(new {
                    message = "Invalid verification code.",
                    details = "Please ensure you're using the correct authenticator app and the time on your device is synchronized. If using Google Authenticator, make sure your phone's time is set to automatic.",
                    possibleIssues = new[] {
                        "The code might be expired - authenticator codes typically last only 30 seconds",
                        "The secret key might not have been scanned correctly",
                        "Your device's time might be out of sync",
                        "You might be using an incorrect verification method"
                    }
                });
            }

            try
            {
                // Store the verified 2FA method based on the code format
                // This is determined automatically by our enhanced validation
                
                var result = await _authService.Enable2FAAsync(user.Id!, true);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to enable 2FA - user not found or database error" });
                }

                // Generate backup codes for account recovery
                var backupCodes = GenerateBackupCodes();
                
                return Ok(new { 
                    message = "2FA enabled successfully",
                    twoFactorEnabled = true,
                    backupCodes = backupCodes,
                    recoveryInstructions = "Please save these backup codes in a secure location. You will need them if you lose access to your authenticator app."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enabling 2FA for user {username}");
                return BadRequest(new { message = $"Error enabling 2FA: {ex.Message}" });
            }
        }

        [HttpPost("2fa/disable")]
        [Authorize]
        public async Task<IActionResult> DisableTwoFactorAuth([FromBody] Verify2FAViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!user.TwoFactorEnabled)
            {
                return BadRequest("2FA is not enabled for this user");
            }

            // Verify the code using proper TOTP validation
            var twoFactorService = HttpContext.RequestServices.GetRequiredService<ITwoFactorService>();
            bool isCodeValid = await twoFactorService.ValidateTOTPCodeAsync(user.TwoFactorSecret ?? string.Empty, model.Code);
            
            if (!isCodeValid)
            {
                Console.WriteLine($"Invalid verification code provided by user {username} for 2FA disable. Code: {model.Code}");
                return BadRequest("Invalid verification code.");
            }

            try
            {
                var result = await _authService.Enable2FAAsync(user.Id!, false);
                if (!result)
                {
                    return BadRequest("Failed to disable 2FA");
                }

                return Ok(new { 
                    message = "2FA disabled successfully",
                    twoFactorEnabled = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error disabling 2FA: {ex.Message}");
            }
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                // Check if email is being changed and if it already exists
                if (user.Email != model.Email && await _userRepository.EmailExistsAsync(model.Email))
                {
                    return BadRequest("Email already exists");
                }

                // Check if username is being changed and if it already exists
                if (user.Username != model.Username && await _userRepository.UsernameExistsAsync(model.Username))
                {
                    return BadRequest("Username already exists");
                }

                // Update user profile
                user.Username = model.Username;
                user.Email = model.Email;

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    // Verify current password first
                    if (string.IsNullOrEmpty(model.CurrentPassword) || 
                        !await _authService.ValidatePasswordAsync(model.CurrentPassword, user.PasswordHash))
                    {
                        return BadRequest("Current password is incorrect");
                    }

                    user.PasswordHash = _authService.HashPassword(model.NewPassword);
                }

                await _userRepository.UpdateAsync(user);

                return Ok(new { 
                    message = "Profile updated successfully",
                    username = user.Username,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating profile: {ex.Message}");
            }
        }

        private bool IsValidTOTPCode(string code)
        {
            // In a real application, you would implement TOTP validation here
            // using libraries like OtpNet or Google.Authenticator
            // For now, we'll do basic validation
            return code.Length == 6 && code.All(char.IsDigit);
        }

        private List<string> GenerateBackupCodes()
        {
            var codes = new List<string>();
            var random = new Random();
            
            for (int i = 0; i < 10; i++)
            {
                var code = random.Next(10000000, 99999999).ToString();
                codes.Add($"{code.Substring(0, 4)}-{code.Substring(4, 4)}");
            }
            
            return codes;
        }

        [HttpPost("debug/test-verify-2fa")]
        public async Task<IActionResult> TestVerify2FA([FromBody] TwoFactorLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetByUsernameAsync(model.Username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new {
                userId = user.Id,
                username = user.Username,
                twoFactorEnabled = user.TwoFactorEnabled,
                twoFactorCode = user.TwoFactorCode,
                twoFactorCodeExpiry = user.TwoFactorCodeExpiry,
                twoFactorCodeUsed = user.TwoFactorCodeUsed,
                receivedCode = model.TwoFactorCode,
                isCodeMatching = user.TwoFactorCode == model.TwoFactorCode,
                isExpired = user.TwoFactorCodeExpiry < DateTime.UtcNow,
                currentUtcTime = DateTime.UtcNow
            });
        }

        [HttpPost("debug/test-2fa-enable")]
        [Authorize]
        public async Task<IActionResult> TestTwoFactorEnable()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new {
                userId = user.Id,
                username = user.Username,
                email = user.Email,
                currentTwoFactorEnabled = user.TwoFactorEnabled,
                hasTwoFactorSecret = !string.IsNullOrEmpty(user.TwoFactorSecret),
                twoFactorSecret = user.TwoFactorSecret
            });
        }

        [HttpPost("debug/set-2fa-code")]
        [Authorize]
        public async Task<IActionResult> SetTwoFactorCode([FromBody] SetTwoFactorCodeViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                // Set 2FA code directly for testing
                user.TwoFactorCode = model.Code;
                user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(model.ExpiryMinutes);
                user.TwoFactorCodeUsed = false;
                
                await _userRepository.UpdateAsync(user);

                return Ok(new { 
                    message = "2FA code set successfully",
                    code = user.TwoFactorCode,
                    expiry = user.TwoFactorCodeExpiry,
                    userId = user.Id,
                    username = user.Username
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error setting 2FA code: {ex.Message}");
            }
        }

        [HttpPost("debug/force-enable-2fa")]
        [Authorize]
        public async Task<IActionResult> ForceEnableTwoFactor()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                // Force enable without code verification for testing
                user.TwoFactorEnabled = true;
                await _userRepository.UpdateAsync(user);

                return Ok(new { 
                    message = "2FA force enabled successfully",
                    twoFactorEnabled = true,
                    userId = user.Id,
                    username = user.Username
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error force enabling 2FA: {ex.Message}");
            }
        }

        [HttpPost("debug/force-disable-2fa")]
        [Authorize]
        public async Task<IActionResult> ForceDisableTwoFactor()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("User not authenticated");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try
            {
                // Force disable 2FA and clear all 2FA data
                user.TwoFactorEnabled = false;
                user.TwoFactorCode = null;
                user.TwoFactorCodeExpiry = null;
                user.TwoFactorCodeUsed = false;
                user.TwoFactorSecret = null;
                
                await _userRepository.UpdateAsync(user);

                return Ok(new { 
                    message = "2FA force disabled successfully",
                    twoFactorEnabled = false,
                    userId = user.Id,
                    username = user.Username
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error force disabling 2FA: {ex.Message}");
            }
        }
        
        [HttpPost("debug/generate-2fa-code")]
        public async Task<IActionResult> GenerateCodeForUser([FromBody] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Username is required");
            }

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return NotFound("User not found");
            }

            try 
            {
                // Generate a new 2FA code
                var code = await _authService.GenerateTwoFactorCodeAsync(user.Id!);
                
                // Get the updated user to return the updated state
                user = await _userRepository.GetByUsernameAsync(username);
                
                return Ok(new {
                    message = "2FA code generated successfully",
                    username = user?.Username ?? username,
                    twoFactorCode = user?.TwoFactorCode,
                    twoFactorCodeExpiry = user?.TwoFactorCodeExpiry,
                    twoFactorCodeUsed = user?.TwoFactorCodeUsed ?? false,
                    currentTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating 2FA code: {ex.Message}");
            }
        }
    }
}

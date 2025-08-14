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
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserRepository userRepository,
            IAuthService authService,
            ITwoFactorService twoFactorService,
            ILogger<UserController> logger)
        {
            _userRepository = userRepository;
            _authService = authService;
            _twoFactorService = twoFactorService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileViewModel>> GetProfile()
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

            return Ok(new UserProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Roles = user.Roles,
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPut("profile")]
        public async Task<ActionResult<UserProfileViewModel>> UpdateProfile(UpdateUserViewModel model)
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

            // Verify current password if changing password
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    return BadRequest("Current password is required to set a new password");
                }

                if (!await _authService.ValidatePasswordAsync(model.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest("Current password is incorrect");
                }

                if (model.NewPassword.Length < 6)
                {
                    return BadRequest("New password must be at least 6 characters long");
                }

                user.PasswordHash = _authService.HashPassword(model.NewPassword);
            }

            // Check if username is being changed and if it's already taken
            if (!string.IsNullOrEmpty(model.Username) && model.Username != user.Username)
            {
                if (await _userRepository.UsernameExistsAsync(model.Username))
                {
                    return BadRequest("Username already exists");
                }
                user.Username = model.Username;
            }

            // Check if email is being changed and if it's already taken
            if (!string.IsNullOrEmpty(model.Email) && model.Email != user.Email)
            {
                if (await _userRepository.EmailExistsAsync(model.Email))
                {
                    return BadRequest("Email already exists");
                }
                user.Email = model.Email;
            }

            // Update other profile fields
            if (!string.IsNullOrEmpty(model.FirstName))
                user.FirstName = model.FirstName;
            
            if (!string.IsNullOrEmpty(model.LastName))
                user.LastName = model.LastName;
            
            if (!string.IsNullOrEmpty(model.Phone))
                user.Phone = model.Phone;

            try
            {
                await _userRepository.UpdateAsync(user);

                return Ok(new UserProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Roles = user.Roles,
                    IsActive = user.IsActive,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user: {Username}", username);
                return BadRequest("Failed to update profile");
            }
        }

        [HttpGet("2fa/status")]
        public async Task<IActionResult> Get2FAStatus()
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

            return Ok(new TwoFactorStatusViewModel
            {
                IsEnabled = user.TwoFactorEnabled,
                HasSecret = !string.IsNullOrEmpty(user.TwoFactorSecret),
                Username = user.Username,
                Email = user.Email
            });
        }

        [HttpPost("2fa/generate-secret")]
        public async Task<IActionResult> Generate2FASecret()
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
                var secret = _twoFactorService.GenerateSecretKey();
                user.TwoFactorSecret = secret;
                await _userRepository.UpdateAsync(user);

                // Generate QR code URL for authenticator apps
                var appName = "SkyElect";
                var label = $"{appName}:{user.Email}";
                var qrCodeUrl = $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(appName)}&algorithm=SHA1&digits=6&period=30";
                var googleChartUrl = $"https://chart.googleapis.com/chart?cht=qr&chs=300x300&chl={Uri.EscapeDataString(qrCodeUrl)}";

                return Ok(new Generate2FASecretViewModel
                {
                    Secret = secret,
                    QrCode = googleChartUrl,
                    ManualEntryKey = secret,
                    AppName = appName,
                    Username = user.Username,
                    Email = user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating 2FA secret for user: {Username}", username);
                return BadRequest("Failed to generate 2FA secret");
            }
        }

        [HttpPost("2fa/enable")]
        public async Task<IActionResult> Enable2FA(Enable2FACodeViewModel model)
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

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return BadRequest("2FA secret not generated. Please generate a secret first.");
            }

            // Verify the code
            var isCodeValid = await _twoFactorService.ValidateTOTPCodeAsync(user.TwoFactorSecret, model.Code);
            if (!isCodeValid)
            {
                return BadRequest("Invalid verification code");
            }

            try
            {
                var result = await _authService.Enable2FAAsync(user.Id!, true);
                if (!result)
                {
                    return BadRequest("Failed to enable 2FA");
                }

                return Ok(new { message = "2FA enabled successfully", twoFactorEnabled = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA for user: {Username}", username);
                return BadRequest("Failed to enable 2FA");
            }
        }

        [HttpPost("2fa/disable")]
        public async Task<IActionResult> Disable2FA(Disable2FAViewModel model)
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
                return BadRequest("2FA is not enabled");
            }

            // Verify the code if user has a secret
            if (!string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                var isCodeValid = await _twoFactorService.ValidateTOTPCodeAsync(user.TwoFactorSecret, model.Code);
                if (!isCodeValid)
                {
                    return BadRequest("Invalid verification code");
                }
            }

            try
            {
                var result = await _authService.Enable2FAAsync(user.Id!, false);
                if (!result)
                {
                    return BadRequest("Failed to disable 2FA");
                }

                return Ok(new { message = "2FA disabled successfully", twoFactorEnabled = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA for user: {Username}", username);
                return BadRequest("Failed to disable 2FA");
            }
        }

        [HttpPost("2fa/verify")]
        public async Task<IActionResult> Verify2FA(Verify2FACodeViewModel model)
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

            if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return BadRequest("2FA is not enabled or configured");
            }

            try
            {
                var isCodeValid = await _twoFactorService.ValidateTOTPCodeAsync(user.TwoFactorSecret, model.Code);
                return Ok(new { isValid = isCodeValid, message = isCodeValid ? "Code is valid" : "Invalid code" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA code for user: {Username}", username);
                return BadRequest("Failed to verify 2FA code");
            }
        }
    }
}


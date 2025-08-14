using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectionApi.Models;
using ElectionApi.Repositories;
using ElectionApi.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElectionApi.Tests
{
    public class Test2FAVerification
    {
        public static async Task Main(string[] args)
        {
            // Set up test dependencies
            var loggerFactory = new NullLoggerFactory();
            var twoFactorService = new TwoFactorService(loggerFactory.CreateLogger<TwoFactorService>());
            var userRepository = new MockUserRepository();
            var authService = new AuthService(userRepository, twoFactorService);

            // Create a test user
            var user = new User
            {
                Id = "1",
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                TwoFactorEnabled = true,
                Roles = new List<string> { "user" }
            };

            userRepository.AddUser(user);

            // Generate a 2FA code
            Console.WriteLine("Generating 2FA code...");
            var code = await authService.GenerateTwoFactorCodeAsync(user.Id);
            Console.WriteLine($"2FA code: {code}");
            Console.WriteLine($"Expiry: {user.TwoFactorCodeExpiry}");

            // Test verification with correct code
            Console.WriteLine("\nVerifying correct code...");
            var isValid = await authService.VerifyTwoFactorCodeAsync(user.Username, code);
            Console.WriteLine($"Valid: {isValid} (Expected: True)");

            // Test verification with incorrect code
            Console.WriteLine("\nVerifying incorrect code...");
            isValid = await authService.VerifyTwoFactorCodeAsync(user.Username, "wrong-code");
            Console.WriteLine($"Valid: {isValid} (Expected: False)");

            // Test verification with expired code
            Console.WriteLine("\nVerifying expired code...");
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(-10);
            await userRepository.UpdateAsync(user);
            isValid = await authService.VerifyTwoFactorCodeAsync(user.Username, code);
            Console.WriteLine($"Valid: {isValid} (Expected: False)");

            // Test verification with used code
            Console.WriteLine("\nVerifying used code...");
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
            user.TwoFactorCodeUsed = true;
            await userRepository.UpdateAsync(user);
            isValid = await authService.VerifyTwoFactorCodeAsync(user.Username, code);
            Console.WriteLine($"Valid: {isValid} (Expected: False)");

            // Test with non-existent user
            Console.WriteLine("\nVerifying code for non-existent user...");
            isValid = await authService.VerifyTwoFactorCodeAsync("nonexistent", code);
            Console.WriteLine($"Valid: {isValid} (Expected: False)");

            Console.WriteLine("\nTests completed.");
        }
    }

    // Simple mock repository for testing
    public class MockUserRepository : IUserRepository
    {
        private Dictionary<string, User> _users = new Dictionary<string, User>();
        private Dictionary<string, User> _usersByUsername = new Dictionary<string, User>();

        public void AddUser(User user)
        {
            _users[user.Id] = user;
            _usersByUsername[user.Username] = user;
        }

        public Task<User> GetByIdAsync(string id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                return Task.FromResult(user);
            }
            return Task.FromResult<User>(null);
        }

        public Task<User> GetByUsernameAsync(string username)
        {
            if (_usersByUsername.TryGetValue(username, out var user))
            {
                return Task.FromResult(user);
            }
            return Task.FromResult<User>(null);
        }

        public Task<User> CreateAsync(User entity)
        {
            _users[entity.Id] = entity;
            _usersByUsername[entity.Username] = entity;
            return Task.FromResult(entity);
        }

        public Task<User> UpdateAsync(User entity)
        {
            _users[entity.Id] = entity;
            _usersByUsername[entity.Username] = entity;
            return Task.FromResult(entity);
        }

        public Task DeleteAsync(string id)
        {
            if (_users.TryGetValue(id, out var user))
            {
                _users.Remove(id);
                _usersByUsername.Remove(user.Username);
            }
            return Task.CompletedTask;
        }

        // Implement required methods for IUserRepository
        public Task<bool> UsernameExistsAsync(string username)
        {
            return Task.FromResult(_usersByUsername.ContainsKey(username));
        }

        public Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(_users.Values.Any(u => u.Email == email));
        }

        public Task<User> GetByEmailAsync(string email)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email == email);
            return Task.FromResult(user);
        }
        
        // These methods need to be implemented for the interface but aren't used in the test
        public Task<IEnumerable<User>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<User>>(_users.Values);
        }
    }
}

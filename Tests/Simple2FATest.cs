using System;
using System.Threading.Tasks;
using ElectionApi.Models;
using ElectionApi.Services;

namespace ElectionApi.Tests
{
    class Simple2FATest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("2FA Verification Test");
            Console.WriteLine("=====================");
            
            // Simulate user data
            var user = new User
            {
                Id = "1",
                Username = "Hannibal",
                Email = "ahlem.bnfradj@gmail.com",
                PasswordHash = "hashed_password",
                TwoFactorEnabled = true,
                TwoFactorCode = "123456",
                TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5),
                TwoFactorCodeUsed = false
            };

            // Setup logger and service
            var logger = new Microsoft.Extensions.Logging.ConsoleLogger<TwoFactorService>();
            var twoFactorService = new TwoFactorService(logger);

            // Test verification
            await TestVerification(user, "123456", true);
            await TestVerification(user, "654321", false);

            // Test with expired code
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(-5);
            await TestVerification(user, "123456", false);

            // Test with used code
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
            user.TwoFactorCodeUsed = true;
            await TestVerification(user, "123456", false);

            // Test with null user
            await TestVerificationWithNullUser("anycode");
        }

        static async Task TestVerification(User user, string code, bool expectedResult)
        {
            Console.WriteLine($"\nTesting code: {code}");
            Console.WriteLine($"User: {user.Username}");
            Console.WriteLine($"TwoFactorCode: {user.TwoFactorCode}");
            Console.WriteLine($"Expiry: {user.TwoFactorCodeExpiry}");
            Console.WriteLine($"Used: {user.TwoFactorCodeUsed}");
            
            bool result = await VerifyTwoFactorCode(user, code);
            
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Expected: {expectedResult}");
            Console.WriteLine($"Test {(result == expectedResult ? "PASSED" : "FAILED")}");
        }

        static async Task TestVerificationWithNullUser(string code)
        {
            Console.WriteLine($"\nTesting with null user, code: {code}");
            
            bool result = await VerifyTwoFactorCode(null, code);
            
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Expected: false");
            Console.WriteLine($"Test {(result == false ? "PASSED" : "FAILED")}");
        }

        static async Task<bool> VerifyTwoFactorCode(User user, string code)
        {
            if (user == null)
            {
                Console.WriteLine("User is null");
                return false;
            }

            if (string.IsNullOrEmpty(user.TwoFactorCode))
            {
                Console.WriteLine("No 2FA code exists");
                return false;
            }

            if (user.TwoFactorCodeExpiry == null || user.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                Console.WriteLine("Code is expired");
                return false;
            }

            if (user.TwoFactorCodeUsed)
            {
                Console.WriteLine("Code already used");
                return false;
            }

            if (user.TwoFactorCode != code)
            {
                Console.WriteLine("Invalid code");
                return false;
            }

            // Mark the code as used (in a real app, you'd update the database here)
            user.TwoFactorCodeUsed = true;
            await Task.CompletedTask; // Simulate async

            Console.WriteLine("Code verified successfully");
            return true;
        }
    }

    // Simple logger implementation for testing
    public class ConsoleLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }
    }
}

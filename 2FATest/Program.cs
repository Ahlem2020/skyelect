using System;
using System.Threading.Tasks;
using ElectionApi.Models;
using ElectionApi.Services;

namespace ElectionApi.Test
{
    class Program
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
                TwoFactorEnabled = true,
                TwoFactorCode = "123456", // Set a test code
                TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5), // Valid for 5 minutes
                TwoFactorCodeUsed = false,
                TwoFactorSecret = "LFBJZAG3PUIZMPU2QCYITD7C3ALLQQIZ"
            };
            
            // Test scenarios
            Console.WriteLine("\nTest Case 1: Valid Code");
            TestVerification(user, "123456", true);
            
            Console.WriteLine("\nTest Case 2: Invalid Code");
            TestVerification(user, "999999", false);
            
            Console.WriteLine("\nTest Case 3: Expired Code");
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(-5); // Expired 5 minutes ago
            TestVerification(user, "123456", false);
            
            Console.WriteLine("\nTest Case 4: Used Code");
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5); // Valid again
            user.TwoFactorCodeUsed = true;
            TestVerification(user, "123456", false);
            
            Console.WriteLine("\nTest Case 5: Null Code");
            user.TwoFactorCode = null;
            TestVerification(user, "123456", false);
            
            Console.WriteLine("\nTest Case 6: Username Not Found");
            TestVerificationWithNullUser("123456");
        }
        
        static void TestVerification(User user, string code, bool expectedResult)
        {
            Console.WriteLine($"  User: {user.Username}");
            Console.WriteLine($"  TwoFactorCode: {user.TwoFactorCode}");
            Console.WriteLine($"  Code to verify: {code}");
            Console.WriteLine($"  TwoFactorCodeExpiry: {user.TwoFactorCodeExpiry}");
            Console.WriteLine($"  TwoFactorCodeUsed: {user.TwoFactorCodeUsed}");
            
            bool result = VerifyTwoFactorCode(user, code);
            
            Console.WriteLine($"  Result: {(result ? "Successful" : "Failed")}");
            Console.WriteLine($"  Expected: {(expectedResult ? "Successful" : "Failed")}");
            Console.WriteLine($"  Test {(result == expectedResult ? "PASSED" : "FAILED")}");
        }
        
        static void TestVerificationWithNullUser(string code)
        {
            Console.WriteLine($"  User: null (not found)");
            Console.WriteLine($"  Code to verify: {code}");
            
            bool result = VerifyTwoFactorCode(null, code);
            
            Console.WriteLine($"  Result: {(result ? "Successful" : "Failed")}");
            Console.WriteLine($"  Expected: Failed");
            Console.WriteLine($"  Test {(!result ? "PASSED" : "FAILED")}");
        }
        
        static bool VerifyTwoFactorCode(User? user, string code)
        {
            // This is similar to the VerifyTwoFactorCodeAsync method but simplified for testing
            if (user == null)
            {
                Console.WriteLine($"  Reason: User not found");
                return false;
            }

            // Check if code exists, hasn't expired, and hasn't been used
            if (string.IsNullOrEmpty(user.TwoFactorCode))
            {
                Console.WriteLine($"  Reason: No 2FA code exists for user");
                return false;
            }
            
            if (user.TwoFactorCodeExpiry == null)
            {
                Console.WriteLine($"  Reason: No expiry time for 2FA code");
                return false;
            }
            
            if (user.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                Console.WriteLine($"  Reason: Code expired at {user.TwoFactorCodeExpiry}");
                return false;
            }
            
            if (user.TwoFactorCodeUsed)
            {
                Console.WriteLine($"  Reason: Code already used");
                return false;
            }
            
            if (user.TwoFactorCode != code)
            {
                Console.WriteLine($"  Reason: Invalid code. Expected '{user.TwoFactorCode}', received '{code}'");
                return false;
            }

            Console.WriteLine($"  Reason: All verification checks passed");
            return true;
        }
    }
}

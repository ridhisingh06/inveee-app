// Temporary utility to generate BCrypt hash
// To run: dotnet run --project invmgmt.web.csproj --password "admin@123"
// Or compile separately and run

using invmgmt.web.Utils;

class Program
{
    static void Main(string[] args)
    {
        var password = "admin@123";
        
        try
        {
            var hash = PasswordUtils.HashPassword(password);
            
            Console.WriteLine("=====================================");
            Console.WriteLine("BCrypt Password Hash Generator");
            Console.WriteLine("=====================================");
            Console.WriteLine($"Password: {password}");
            Console.WriteLine();
            Console.WriteLine($"Hash: {hash}");
            Console.WriteLine();
            Console.WriteLine("SQL Update Command:");
            Console.WriteLine($@"UPDATE ""User"" SET ""PasswordHash"" = '{hash}' WHERE ""Email"" = 'admin@gmail.com';");
            Console.WriteLine();
            
            // Verify it works
            bool verified = PasswordUtils.VerifyPassword(password, hash);
            Console.WriteLine($"Verification Test: {(verified ? "✓ PASS" : "✗ FAIL")}");
            Console.WriteLine("=====================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

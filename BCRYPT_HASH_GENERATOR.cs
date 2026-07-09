// ============================================================================
// BCrypt Hash Generator
// ============================================================================
// To use this code:
// 1. Create a new .NET console project: dotnet new console -n HashGen
// 2. Add BCrypt.Net: dotnet add package BCrypt.Net-Next --version 4.2.0
// 3. Replace Program.cs with this code
// 4. Run: dotnet run
// ============================================================================

using BCrypt.Net;

var password = "admin@123";
var hash = BCrypt.HashPassword(password, workFactor: 11);

Console.WriteLine("Password: admin@123");
Console.WriteLine("BCrypt Hash (workFactor 11):");
Console.WriteLine(hash);
Console.WriteLine();

// Verify it works
bool isValid = BCrypt.Verify(password, hash);
Console.WriteLine($"Verification test: {(isValid ? "✓ PASS" : "✗ FAIL")}");
Console.WriteLine();
Console.WriteLine("Use this hash in your database:");
Console.WriteLine($"UPDATE \"User\" SET \"PasswordHash\" = '{hash}' WHERE \"Email\" = 'admin@gmail.com';");

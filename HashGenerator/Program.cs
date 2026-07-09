using BCrypt.Net;

var password = "admin@123";
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

Console.WriteLine("=== BCrypt Hash Generator ===");
Console.WriteLine($"Password: {password}");
Console.WriteLine();
Console.WriteLine($"Generated Hash:");
Console.WriteLine(hash);
Console.WriteLine();
Console.WriteLine("SQL Query to update admin password:");
Console.WriteLine($"UPDATE \"User\" SET \"PasswordHash\" = '{hash}' WHERE \"Email\" = 'admin@gmail.com';");
Console.WriteLine();

// Verify it works
if (BCrypt.Net.BCrypt.Verify(password, hash))
{
    Console.WriteLine("✓ Verification: PASS - Hash is valid and can be used");
}
else
{
    Console.WriteLine("✗ Verification: FAIL - Hash is invalid");
}

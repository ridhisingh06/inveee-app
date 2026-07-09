#!/usr/bin/env dotnet-script
// hash_gen.cs - Simple BCrypt hash generator
// Usage: dotnet script run hash_gen.cs

#r "nuget: BCrypt.Net-Next, 4.2.0"

using BCrypt.Net;

var password = "admin@123";
var hash = BCrypt.HashPassword(password);

Console.WriteLine("===== BCrypt Hash Generator =====");
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine();
Console.WriteLine("SQL Query to update admin password:");
Console.WriteLine($@"UPDATE ""User"" SET ""PasswordHash"" = '{hash}' WHERE ""Email"" = 'admin@gmail.com';");

// Verify
var isValid = BCrypt.Verify(password, hash);
Console.WriteLine($"\nVerification: {(isValid ? "✓ PASS" : "✗ FAIL")}");

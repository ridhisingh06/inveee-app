using System.Security.Cryptography;
using System.Text;

namespace invmgmt.web.Utils
{
    public static class PasswordUtils
    {
        public static bool LooksLikeBcryptHash(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$");
        }

        public static bool FixedTimeEquals(string a, string b)
        {
            // Compare as UTF8 bytes to avoid leaking timing for legacy plaintext passwords.
            var aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
            var bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);

            if (aBytes.Length != bBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        /// <summary>
        /// Hash password using BCrypt
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verify password using BCrypt
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash)) 
                return false;
                
            if (!LooksLikeBcryptHash(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}


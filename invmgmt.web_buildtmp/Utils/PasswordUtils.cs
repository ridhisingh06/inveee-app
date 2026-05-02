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
    }
}


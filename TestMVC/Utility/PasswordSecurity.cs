using System;
using System.Security.Cryptography;

namespace Utility
{
    public static class PasswordSecurity
    {
        private const int SaltSize = 32;       // 256-bit salt
        private const int KeySize = 64;        // 512-bit hash
        private const int Iterations = 100000; // 100,000 PBKDF2 iterations
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

        /// <summary>
        /// Creates a cryptographically secure Salt and PBKDF2 SHA-512 Hash for the given password.
        /// </summary>
        public static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                HashAlgorithm,
                KeySize);

            passwordSalt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Verifies a password against the stored PBKDF2 SHA-512 hash and salt using constant-time comparison.
        /// </summary>
        public static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
            {
                return false;
            }

            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                byte[] storedHashBytes = Convert.FromBase64String(storedHash);

                byte[] computedHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    saltBytes,
                    Iterations,
                    HashAlgorithm,
                    KeySize);

                return CryptographicOperations.FixedTimeEquals(computedHashBytes, storedHashBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}

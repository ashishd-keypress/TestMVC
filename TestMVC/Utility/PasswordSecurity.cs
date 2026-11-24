using System;
using BCrypt.Net;

namespace Utility
{
    public static class PasswordSecurity
    {
        // Work factor 12 produces standard $2a$12$... hashes with 4,096 key derivation rounds
        private const int WorkFactor = 12;

        /// <summary>
        /// Hashes a plain-text password using BCrypt with workFactor 12.
        /// Resulting hash format: $2a$12$... (includes algorithm, cost, salt, and hash in 60 chars).
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
        }

        /// <summary>
        /// Verifies a plain-text password against a stored BCrypt hash.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}

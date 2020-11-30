using SmartFriends.Api.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SmartFriends.Api.Helpers
{
    public static class LoginHelper
    {
        public static string CalculateDigest(string password, SaltInfo info)
        {
            var hashedPassword = GetHash(SHA256.Create(), password, info.Salt);
            return GetHash(SHA1.Create(), hashedPassword, info.SessionSalt);
        }

        private static string GetHash(HashAlgorithm algorithm, string password, string salt)
        {
            var saltArray = Convert.FromBase64String(salt);
            var passwordArray = Encoding.UTF8.GetBytes(password);

            var pasConSalt = new byte[saltArray.Length + passwordArray.Length];
            Array.Copy(passwordArray, pasConSalt, passwordArray.Length);
            Array.Copy(saltArray, 0, pasConSalt, passwordArray.Length, saltArray.Length);
            var cryptHash = algorithm.ComputeHash(pasConSalt);
            return Convert.ToBase64String(cryptHash);
        }
    }
}

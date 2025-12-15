using System.Security.Cryptography;
using System.Text;

namespace glnc_webpart.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const string Salt = "GLNC_Delivery_Management_2024"; // Application salt

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            // Combine password with salt
            string saltedPassword = password + Salt;

            // Compute SHA256 hash
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(saltedPassword);
                byte[] hash = sha256.ComputeHash(bytes);
                
                // Convert byte array to hexadecimal string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }
                
                return builder.ToString();
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                return false;
            }

            try
            {
                // First, try with salt (current method)
                string hashedInputWithSalt = HashPassword(password);
                if (hashedInputWithSalt.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // If that doesn't match, try without salt (for backward compatibility with existing passwords)
                // This handles passwords that were hashed before salt was added
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(password);
                    byte[] hash = sha256.ComputeHash(bytes);
                    
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        builder.Append(hash[i].ToString("x2"));
                    }
                    
                    string hashedInputWithoutSalt = builder.ToString();
                    bool matches = hashedInputWithoutSalt.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
                    
                    // If password matches without salt, optionally re-hash with salt for future use
                    // (This would require updating the database, which we don't do here)
                    
                    return matches;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}


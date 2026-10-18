using BCrypt.Net;

namespace AtmSimulator.Services.Security
{
    public class BCryptPinHasher : IPinHasher
    {
        public string Hash(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("PIN не може бути порожнім", nameof(pin));

            return BCrypt.Net.BCrypt.EnhancedHashPassword(pin, HashType.SHA384);
        }

        public bool Verify(string pin, string hash)
        {
            if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(hash))
                return false;

            // Підтримка плавної міграції: якщо в базі ще старий PIN відкритим текстом (4 цифри)
            if (hash.Length == 4 && hash.All(char.IsDigit))
            {
                return hash == pin;
            }

            try
            {
                return BCrypt.Net.BCrypt.EnhancedVerify(pin, hash, HashType.SHA384);
            }
            catch
            {
                // Резервний варіант для стандартного формату BCrypt
                try
                {
                    return BCrypt.Net.BCrypt.Verify(pin, hash);
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool NeedsRehash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return true;

            // Якщо хеш має вигляд звичайного 4-значного PIN, його потрібно оновити на безпечний BCrypt хеш
            return hash.Length == 4 && hash.All(char.IsDigit);
        }
    }
}

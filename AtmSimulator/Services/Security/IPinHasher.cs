namespace AtmSimulator.Services.Security
{
    public interface IPinHasher
    {
        /// <summary>
        /// Створює криптографічний хеш для заданого PIN-коду з використанням солі.
        /// </summary>
        string Hash(string pin);

        /// <summary>
        /// Перевіряє відповідність введеного PIN-коду збереженому хешу.
        /// Підтримує також безпечну перевірку legacy (відкритих) PIN для плавної міграції.
        /// </summary>
        bool Verify(string pin, string hash);

        /// <summary>
        /// Перевіряє, чи потребує хеш оновлення (наприклад, якщо в базі ще зберігався старий відкритий PIN).
        /// </summary>
        bool NeedsRehash(string hash);
    }
}

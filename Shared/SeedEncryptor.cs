using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace QrCodeGenerator
{
    internal static class SeedEncryptor
    {
        private const int SALT_SIZE = 32;
        private const int KEY_SIZE = 32; // 256 bits
        private const int IV_SIZE = 16; // 128 bits
        private const int ITERATIONS = 600000;
        private const int MAX_PLAINTEXT_LENGTH = 1024; // Максимальна довжина вхідного тексту
        private const byte ENCRYPTION_VERSION = 1; // Версія формату шифрування

        public static string Encrypt(string seedPhrase, string password)
        {
            if (string.IsNullOrEmpty(seedPhrase))
                throw new ArgumentException("Сід-фраза не може бути порожньою");

            if (seedPhrase.Length > MAX_PLAINTEXT_LENGTH)
                throw new ArgumentException($"Сід-фраза занадто довга (максимум {MAX_PLAINTEXT_LENGTH} символів)");

            ValidatePassword(password);

            try
            {
                // Generate a random salt
                byte[] salt = new byte[SALT_SIZE];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                // Derive a key from the password
                byte[] key;
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA256))
                {
                    key = pbkdf2.GetBytes(KEY_SIZE);
                }

                // Encrypt the data
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.Key = key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using var encryptor = aes.CreateEncryptor();
                byte[] inputBytes = Encoding.UTF8.GetBytes(seedPhrase);

                using var msEncrypt = new MemoryStream();
                
                // Write version and salt and IV first
                msEncrypt.WriteByte(ENCRYPTION_VERSION);
                msEncrypt.Write(salt, 0, salt.Length);
                msEncrypt.Write(iv, 0, iv.Length);

                // Encrypt the data
                using (var cryptoStream = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                }

                // Get the encrypted data as a base64 string
                byte[] encryptedData = msEncrypt.ToArray();
                return Convert.ToBase64String(encryptedData);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Помилка шифрування", ex);
            }
        }

        public static string Decrypt(string encryptedText, string password)
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("Зашифрований текст не може бути порожнім");

            ValidatePassword(password);

            try
            {
                // Decode the base64 string
                byte[] encryptedData = Convert.FromBase64String(encryptedText);

                if (encryptedData.Length < 1 + SALT_SIZE + IV_SIZE)
                    throw new CryptographicException("Невірний формат зашифрованих даних");

                // Extract version
                byte version = encryptedData[0];
                if (version != ENCRYPTION_VERSION)
                    throw new CryptographicException($"Непідтримувана версія шифрування: {version}");

                // Extract salt and IV
                byte[] salt = new byte[SALT_SIZE];
                byte[] iv = new byte[IV_SIZE];
                Buffer.BlockCopy(encryptedData, 1, salt, 0, SALT_SIZE);
                Buffer.BlockCopy(encryptedData, 1 + SALT_SIZE, iv, 0, IV_SIZE);

                // Derive the key from the password
                byte[] key;
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA256))
                {
                    key = pbkdf2.GetBytes(KEY_SIZE);
                }

                // Get the encrypted data without version, salt and IV
                int encryptedLength = encryptedData.Length - (1 + SALT_SIZE + IV_SIZE);
                byte[] encryptedContent = new byte[encryptedLength];
                Buffer.BlockCopy(encryptedData, 1 + SALT_SIZE + IV_SIZE, encryptedContent, 0, encryptedLength);

                // Decrypt the data
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.IV = iv;
                aes.Key = key;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(encryptedContent);
                using var cryptoStream = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                string decryptedText = reader.ReadToEnd();

                if (string.IsNullOrEmpty(decryptedText))
                    throw new CryptographicException("Помилка розшифрування: отримано порожній результат");

                return decryptedText;
            }
            catch (FormatException)
            {
                throw new CryptographicException("Невірний формат зашифрованих даних");
            }
            catch (CryptographicException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Помилка розшифрування", ex);
            }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не може бути порожнім");

            if (password.Length < 12)
                throw new ArgumentException("Пароль повинен містити мінімум 12 символів");

            // Перевірка на наявність різних типів символів
            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            bool hasLower = Regex.IsMatch(password, "[a-z]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSpecial = Regex.IsMatch(password, "[^A-Za-z0-9]");

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
                throw new ArgumentException("Пароль повинен містити великі та малі літери, цифри та спеціальні символи");
        }
    }
}

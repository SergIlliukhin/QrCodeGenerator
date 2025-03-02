using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

public class SeedEncryptor
{
    private const int SALT_SIZE = 32;
    private const int KEY_SIZE = 32; // 256 bits
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

            // Generate key using PBKDF2
            byte[] key = new byte[KEY_SIZE];
            try
            {
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512);
                key = deriveBytes.GetBytes(KEY_SIZE);

                // Generate a random IV
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.GenerateIV();

                // Encrypt the data
                using var encryptor = aes.CreateEncryptor(key, aes.IV);
                byte[] inputBytes = Encoding.UTF8.GetBytes(seedPhrase);

                using var msEncrypt = new MemoryStream();
                
                // Write version and salt and IV first
                msEncrypt.WriteByte(ENCRYPTION_VERSION);
                msEncrypt.Write(salt, 0, salt.Length);
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                // Encrypt the data
                using (var cryptoStream = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(inputBytes, 0, inputBytes.Length);
                    cryptoStream.FlushFinalBlock();
                }

                // Get the encrypted data
                byte[] encryptedData = msEncrypt.ToArray();

                // Calculate HMAC
                byte[] hmacInput = new byte[encryptedData.Length];
                Buffer.BlockCopy(encryptedData, 0, hmacInput, 0, encryptedData.Length);

                byte[] tag;
                using (var hmac = new HMACSHA512(key))
                {
                    tag = hmac.ComputeHash(hmacInput);
                }

                // Combine all parts
                byte[] result = new byte[encryptedData.Length + tag.Length];
                Buffer.BlockCopy(encryptedData, 0, result, 0, encryptedData.Length);
                Buffer.BlockCopy(tag, 0, result, encryptedData.Length, tag.Length);

                return Convert.ToBase64String(result);
            }
            finally
            {
                // Безпечне очищення чутливих даних
                if (key != null)
                    CryptographicOperations.ZeroMemory(key);
            }
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Помилка при шифруванні даних", ex);
        }
    }

    public static string Decrypt(string encryptedText, string password)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Зашифрований текст не може бути порожнім");

        ValidatePassword(password);

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            if (encryptedBytes.Length < SALT_SIZE + 17 + 64) // version + salt + minimal IV + minimal encrypted data + HMAC
                throw new CryptographicException("Некоректний формат зашифрованих даних");

            // Extract version, salt, IV, and tag
            byte version = encryptedBytes[0];
            if (version != ENCRYPTION_VERSION)
                throw new CryptographicException($"Непідтримувана версія шифрування: {version}");

            byte[] salt = new byte[SALT_SIZE];
            byte[] iv = new byte[16]; // AES block size
            byte[] tag = new byte[64]; // HMACSHA512 size
            byte[] encryptedData = new byte[encryptedBytes.Length - 1 - SALT_SIZE - 16 - 64];

            using (var ms = new MemoryStream(encryptedBytes))
            {
                ms.Position = 1; // Skip version
                ms.Read(salt, 0, SALT_SIZE);
                ms.Read(iv, 0, 16);
                ms.Read(encryptedData, 0, encryptedData.Length);
                ms.Read(tag, 0, 64);
            }

            // Derive the key
            byte[] key = new byte[KEY_SIZE];
            try
            {
                using var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512);
                key = deriveBytes.GetBytes(KEY_SIZE);

                // Verify HMAC
                using (var hmac = new HMACSHA512(key))
                {
                    var hmacInput = new byte[1 + SALT_SIZE + 16 + encryptedData.Length];
                    hmacInput[0] = version;
                    Buffer.BlockCopy(salt, 0, hmacInput, 1, SALT_SIZE);
                    Buffer.BlockCopy(iv, 0, hmacInput, 1 + SALT_SIZE, 16);
                    Buffer.BlockCopy(encryptedData, 0, hmacInput, 1 + SALT_SIZE + 16, encryptedData.Length);

                    var computedTag = hmac.ComputeHash(hmacInput);
                    if (!CryptographicOperations.FixedTimeEquals(computedTag, tag))
                    {
                        throw new CryptographicException("❌ Невірний пароль або дані пошкоджені");
                    }
                }

                // Decrypt the data
                using var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                
                using var decryptor = aes.CreateDecryptor(key, iv);
                using var msDecrypt = new MemoryStream(encryptedData);
                using var cryptoStream = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
                
                return reader.ReadToEnd();
            }
            finally
            {
                // Безпечне очищення чутливих даних
                if (key != null)
                    CryptographicOperations.ZeroMemory(key);
            }
        }
        catch (FormatException)
        {
            throw new CryptographicException("❌ Некоректний формат зашифрованих даних");
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("❌ " + ex.Message);
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

#if NET6_0_OR_GREATER
using System.Security.Cryptography;
#else
using System.Security.Cryptography.Aes;
using System.Security.Cryptography;
#endif
using System;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;


public class SeedEncryptor
{
    private const int SALT_SIZE = 32;
    private const int KEY_SIZE = 32; // 256 bits
    private const int ITERATIONS = 600000;
    private const int MAX_PLAINTEXT_LENGTH = 1024; // Максимальна довжина вхідного тексту
    private const byte ENCRYPTION_VERSION = 2; // Версія формату шифрування (оновлено для AES-GCM)
    private const int NONCE_SIZE = 12; // Розмір nonce для AES-GCM
    private const int TAG_SIZE = 16; // Розмір тегу автентифікації для AES-GCM

    public static string Encrypt(string seedPhrase, string password)
    {
        if (string.IsNullOrEmpty(seedPhrase))
            throw new ArgumentException("Сід-фраза не може бути порожньою");

        if (seedPhrase.Length > MAX_PLAINTEXT_LENGTH)
            throw new ArgumentException($"Сід-фраза занадто довга (максимум {MAX_PLAINTEXT_LENGTH} символів)");

        ValidatePassword(password);

        byte[] key = null;
        try
        {
            // Generate a random salt
            byte[] salt = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Generate a random nonce
            byte[] nonce = new byte[NONCE_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            // Generate key using PBKDF2
            key = new byte[KEY_SIZE];
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512))
            {
                key = deriveBytes.GetBytes(KEY_SIZE);
            }

            byte[] tag = new byte[TAG_SIZE];
            byte[] inputBytes = Encoding.UTF8.GetBytes(seedPhrase);
            byte[] ciphertext = new byte[inputBytes.Length];

            // Encrypt using AES-GCM
#if NET6_0_OR_GREATER
            using (var aes = new AesGcm(key))
            {
                aes.Encrypt(nonce, inputBytes, ciphertext, tag);
            }
#else
            throw new PlatformNotSupportedException("AES-GCM encryption requires .NET 6.0 or later. Please upgrade your project to use this feature.");
#endif

            // Combine all parts: version + salt + nonce + tag + ciphertext
            using (var msEncrypt = new MemoryStream())
            {
                msEncrypt.WriteByte(ENCRYPTION_VERSION);
                msEncrypt.Write(salt, 0, salt.Length);
                msEncrypt.Write(nonce, 0, nonce.Length);
                msEncrypt.Write(tag, 0, tag.Length);
                msEncrypt.Write(ciphertext, 0, ciphertext.Length);

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Помилка при шифруванні даних", ex);
        }
        finally
        {
            // Безпечне очищення чутливих даних
            if (key != null)
                CryptographicOperations.ZeroMemory(key);
        }
    }

    public static string Decrypt(string encryptedText, string password)
    {
        if (string.IsNullOrEmpty(encryptedText))
            throw new ArgumentException("Зашифрований текст не може бути порожнім");

        ValidatePassword(password);

        byte[] key = null;
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            if (encryptedBytes.Length < 1 + SALT_SIZE + NONCE_SIZE + TAG_SIZE) // version + salt + nonce + tag
                throw new CryptographicException("Некоректний формат зашифрованих даних");

            using var ms = new MemoryStream(encryptedBytes);
            
            // Extract version
            byte version = (byte)ms.ReadByte();
            if (version != ENCRYPTION_VERSION)
            {
                if (version == 1)
                    return DecryptLegacyV1(encryptedText, password);
                throw new CryptographicException($"Непідтримувана версія шифрування: {version}");
            }

            // Extract components
            byte[] salt = new byte[SALT_SIZE];
            byte[] nonce = new byte[NONCE_SIZE];
            byte[] tag = new byte[TAG_SIZE];
            ms.Read(salt, 0, SALT_SIZE);
            ms.Read(nonce, 0, NONCE_SIZE);
            ms.Read(tag, 0, TAG_SIZE);

            // Read the ciphertext
            byte[] ciphertext = new byte[encryptedBytes.Length - 1 - SALT_SIZE - NONCE_SIZE - TAG_SIZE];
            ms.Read(ciphertext, 0, ciphertext.Length);

            // Derive the key
            key = new byte[KEY_SIZE];
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512))
            {
                key = deriveBytes.GetBytes(KEY_SIZE);
            }

            // Decrypt using AES-GCM
            byte[] plaintext = new byte[ciphertext.Length];
#if NET6_0_OR_GREATER
            using (var aes = new AesGcm(key))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }
#else
            throw new PlatformNotSupportedException("AES-GCM decryption requires .NET 6.0 or later. Please upgrade your project to use this feature.");
#endif

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (FormatException)
        {
            throw new CryptographicException("❌ Некоректний формат зашифрованих даних");
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("❌ " + ex.Message);
        }
        finally
        {
            // Безпечне очищення чутливих даних
            if (key != null)
                CryptographicOperations.ZeroMemory(key);
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

    private static string DecryptLegacyV1(string encryptedText, string password)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        if (encryptedBytes.Length < SALT_SIZE + 17 + 64) // version + salt + minimal IV + minimal encrypted data + HMAC
            throw new CryptographicException("Некоректний формат зашифрованих даних");

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
        byte[] key = null;
        try
        {
            key = new byte[KEY_SIZE];
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512))
            {
                key = deriveBytes.GetBytes(KEY_SIZE);
            }

            // Verify HMAC
            using (var hmac = new HMACSHA512(key))
            {
                var hmacInput = new byte[1 + SALT_SIZE + 16 + encryptedData.Length];
                hmacInput[0] = 1; // Version 1
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
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                
                using (var decryptor = aes.CreateDecryptor(key, iv))
                using (var msDecrypt = new MemoryStream(encryptedData))
                using (var cryptoStream = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        finally
        {
            if (key != null)
                CryptographicOperations.ZeroMemory(key);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class SeedEncryptor
{
    private const int SALT_SIZE = 32;
    private const int KEY_SIZE = 32; // 256 bits
    private const int ITERATIONS = 600000;

    public static string Encrypt(string seedPhrase, string password)
    {
        // Generate a random salt
        byte[] salt = new byte[SALT_SIZE];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Generate key using PBKDF2
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512);
        byte[] key = deriveBytes.GetBytes(KEY_SIZE);

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
        
        // Write salt and IV first
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

    public static string Decrypt(string encryptedText, string password)
    {
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            // Extract salt, IV, and tag
            byte[] salt = new byte[SALT_SIZE];
            byte[] iv = new byte[16]; // AES block size
            byte[] tag = new byte[64]; // HMACSHA512 size
            byte[] encryptedData = new byte[encryptedBytes.Length - SALT_SIZE - 16 - 64];

            using (var ms = new MemoryStream(encryptedBytes))
            {
                ms.Read(salt, 0, SALT_SIZE);
                ms.Read(iv, 0, 16);
                ms.Read(encryptedData, 0, encryptedData.Length);
                ms.Read(tag, 0, 64);
            }

            // Derive the key
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, ITERATIONS, HashAlgorithmName.SHA512);
            byte[] key = deriveBytes.GetBytes(KEY_SIZE);

            // Verify HMAC
            using (var hmac = new HMACSHA512(key))
            {
                var hmacInput = new byte[SALT_SIZE + 16 + encryptedData.Length];
                Buffer.BlockCopy(salt, 0, hmacInput, 0, SALT_SIZE);
                Buffer.BlockCopy(iv, 0, hmacInput, SALT_SIZE, 16);
                Buffer.BlockCopy(encryptedData, 0, hmacInput, SALT_SIZE + 16, encryptedData.Length);

                var computedTag = hmac.ComputeHash(hmacInput);
                if (!CryptographicOperations.FixedTimeEquals(computedTag, tag))
                {
                    throw new CryptographicException("Authentication failed - data may have been tampered with");
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
        catch (CryptographicException)
        {
            throw new CryptographicException("❌ Невірний пароль або пошкоджені дані");
        }
    }
}

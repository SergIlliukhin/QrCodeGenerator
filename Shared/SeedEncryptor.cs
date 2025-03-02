using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class SeedEncryptor
{
    public static string Encrypt(string seedPhrase, string password)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = DeriveKey(password, aes.KeySize / 8);
            aes.IV = DeriveKey(password, aes.BlockSize / 8);

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(seedPhrase);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }
    }

    public static string Decrypt(string encryptedText, string password)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = DeriveKey(password, aes.KeySize / 8);
            aes.IV = DeriveKey(password, aes.BlockSize / 8);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }

    private static byte[] DeriveKey(string password, int keySize)
    {
        byte[] salt = Encoding.UTF8.GetBytes("SALT_VALUE");
        using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 310000, HashAlgorithmName.SHA256))
        {
            return deriveBytes.GetBytes(keySize);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;

class DecryptorProgram
{
    static void Main()
    {
        try
        {
            Console.Write("Введіть шлях до QR-коду: ");
            string qrFilePath = Console.ReadLine();

            string encryptedSeed;
            try
            {
                encryptedSeed = QRCodeReaderUtil.ReadQRCode(qrFilePath);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            Console.Write("Введіть секретний пароль: ");
            string password = Console.ReadLine();

            try
            {
                string decryptedSeed = SeedEncryptor.Decrypt(encryptedSeed, password);
                Console.WriteLine($"✅ Відновлена сід-фраза: {decryptedSeed}");
            }
            catch (CryptographicException)
            {
                Console.WriteLine("❌ Невірний пароль");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Помилка розшифрування: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Неочікувана помилка: {ex.Message}");
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using QrCodeGenerator.Shared;

namespace QrCodeGenerator.Reader
{
    internal static class DecryptorProgram
    {
        private static readonly CompositeFormat SimpleFormat = CompositeFormat.Parse("{0}");
        private static readonly CompositeFormat DecryptedSeedFormat = CompositeFormat.Parse("✅ Відновлена сід-фраза: {0}");
        private static readonly CompositeFormat ValidationErrorFormat = CompositeFormat.Parse("❌ Помилка валідації: {0}");
        private static readonly CompositeFormat EncryptionErrorFormat = CompositeFormat.Parse("❌ Помилка розшифрування: {0}");

        public static void Main()
    {
        try
        {
            Console.Write(string.Format(CultureInfo.InvariantCulture, SimpleFormat, "Введіть шлях до QR-коду: "));
            string qrFilePath = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(qrFilePath))
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, "Шлях до QR-коду не може бути порожнім"));
                return;
            }

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

            Console.Write(string.Format(CultureInfo.InvariantCulture, SimpleFormat, "Введіть секретний пароль: "));
            string password = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, "Пароль не може бути порожнім"));
                return;
            }

            try
            {
                string decryptedSeed = SeedEncryptor.Decrypt(encryptedSeed, password);
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, DecryptedSeedFormat, decryptedSeed));
            }
            catch (CryptographicException)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, "❌ Невірний пароль"));
            }
            catch (FormatException ex)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, EncryptionErrorFormat, ex.Message));
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, ex.Message));
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, "Немає доступу до файлу"));
        }
    }
}}

using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace QrCodeGenerator
{
    sealed class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Need to catch all exceptions in the main entry point to prevent application crash")]
        static void Main()
        {
            try
            {
                Console.WriteLine("Що ви хочете зробити?");
                Console.WriteLine("1. Згенерувати QR-код для сід-фрази (шифрування)");
                Console.WriteLine("2. Розшифрувати QR-код з сід-фразою");
                Console.Write("\nВведіть номер опції (1 або 2): ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        EncryptAndGenerateQR();
                        break;
                    case "2":
                        DecryptQR();
                        break;
                    default:
                        Console.WriteLine("❌ Невірний вибір. Будь ласка, введіть 1 або 2.");
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"❌ Помилка валідації: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"❌ Помилка вводу/виводу: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"❌ Помилка доступу: {ex.Message}");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"❌ Помилка шифрування: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Несподівана помилка: {ex.Message}");
                Console.WriteLine("Будь ласка, повідомте про цю помилку розробникам.");
            }
        }

        private static void EncryptAndGenerateQR()
        {
            string seedPhrase;
            bool isSeedValid = false;
            do
            {
                Console.WriteLine("\nВимоги до сід-фрази:");
                Console.WriteLine("- Повинна складатися з 12 або 24 слів");
                Console.WriteLine("- Слова повинні бути розділені одним пробілом");
                Console.WriteLine("- Тільки слова зі стандартного словника BIP-39");
                Console.WriteLine("- Всі слова повинні бути в нижньому регістрі");
                
                Console.Write("\nВведіть сід-фразу: ");
                seedPhrase = Console.ReadLine() ?? string.Empty;

                try
                {
                    ValidateSeedPhrase(seedPhrase);
                    isSeedValid = true;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
            } while (!isSeedValid);

            string password;
            bool isPasswordValid = false;
            do
            {
                Console.WriteLine("\nВимоги до пароля:");
                Console.WriteLine("- Мінімум 12 символів");
                Console.WriteLine("- Хоча б одна велика літера (A-Z)");
                Console.WriteLine("- Хоча б одна мала літера (a-z)");
                Console.WriteLine("- Хоча б одна цифра (0-9)");
                Console.WriteLine("- Хоча б один спеціальний символ (!@#$%^&* тощо)");
                
                Console.Write("\nВведіть секретний пароль: ");
                password = Console.ReadLine() ?? string.Empty;

                try
                {
                    ValidatePasswordFormat(password);
                    isPasswordValid = true;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
            } while (!isPasswordValid);

            Console.Write("\nВведіть назву файлу для QR-коду (або натисніть Enter для значення за замовчуванням 'seed_qr.png'): ");
            string? qrFilePath = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(qrFilePath))
            {
                qrFilePath = "seed_qr.png";
            }
            else if (!qrFilePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                qrFilePath += ".png";
            }

            string encryptedSeed = SeedEncryptor.Encrypt(seedPhrase, password);
            Console.WriteLine($"\n🔐 Зашифрована сід-фраза: {encryptedSeed}");

            QRCodeGeneratorUtil.GenerateQRCode(encryptedSeed, qrFilePath);
            Console.WriteLine($"✅ QR-код збережено у {qrFilePath}");
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Need to catch all exceptions to provide user-friendly error messages")]
        private static void DecryptQR()
        {
            try
            {
                Console.Write("\nВведіть шлях до QR-коду: ");
                var qrFilePath = Console.ReadLine()?.Trim() ?? throw new ArgumentException("Шлях до файлу не може бути порожнім");

                if (!File.Exists(qrFilePath))
                {
                    throw new FileNotFoundException("Файл не знайдено", qrFilePath);
                }

                var encryptedSeed = QRCodeReaderUtil.ReadQRCode(qrFilePath);
                if (string.IsNullOrEmpty(encryptedSeed))
                {
                    throw new InvalidOperationException("Не вдалося прочитати QR-код");
                }

                Console.Write("\nВведіть пароль для розшифрування: ");
                var password = Console.ReadLine() ?? throw new ArgumentException("Пароль не може бути порожнім");

                ValidatePasswordFormat(password);

                var decryptedSeed = SeedEncryptor.Decrypt(encryptedSeed, password);
                Console.WriteLine($"\n✅ Розшифрована сід-фраза: {decryptedSeed}");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"❌ Файл не знайдено: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"❌ Помилка валідації: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"❌ Помилка операції: {ex.Message}");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"❌ Помилка розшифрування: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"❌ Помилка вводу/виводу: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Несподівана помилка: {ex.Message}");
                Console.WriteLine("Будь ласка, повідомте про цю помилку розробникам.");
            }
        }

        private static void ValidateSeedPhrase(string seedPhrase)
        {
            if (string.IsNullOrEmpty(seedPhrase))
            {
                throw new ArgumentException("Сід-фраза не може бути порожньою");
            }

            if (seedPhrase.StartsWith(' ') || seedPhrase.EndsWith(' ') || seedPhrase.Contains("  ", StringComparison.Ordinal))
            {
                throw new ArgumentException("Сід-фраза не повинна містити пробіли на початку/в кінці або подвійні пробіли");
            }

            var words = seedPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length != 12 && words.Length != 24)
            {
                throw new ArgumentException("Сід-фраза повинна містити 12 або 24 слова");
            }

            if (!words.All(word => Bip39Words.IsValidWord(word)))
            {
                throw new ArgumentException("Сід-фраза містить недійсні слова. Використовуйте тільки слова зі стандартного списку BIP-39");
            }
        }

        private static void ValidatePasswordFormat(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не може бути порожнім");

            var errors = new System.Collections.Generic.List<string>();

            if (password.Length < 12)
                errors.Add("Пароль закороткий (потрібно мінімум 12 символів)");

            if (!Regex.IsMatch(password, "[A-Z]"))
                errors.Add("Пароль повинен містити хоча б одну велику літеру");

            if (!Regex.IsMatch(password, "[a-z]"))
                errors.Add("Пароль повинен містити хоча б одну малу літеру");

            if (!Regex.IsMatch(password, "[0-9]"))
                errors.Add("Пароль повинен містити хоча б одну цифру");

            if (!Regex.IsMatch(password, "[^A-Za-z0-9]"))
                errors.Add("Пароль повинен містити хоча б один спеціальний символ");

            if (errors.Count > 0)
                throw new ArgumentException(string.Join("\n", errors));
        }
    }
}

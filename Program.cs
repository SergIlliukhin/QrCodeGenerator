using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#nullable enable

namespace QrCodeGenerator
{
    sealed class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Need to catch all exceptions in the main entry point to prevent application crash")]
        static void Main()
        {
            bool continueProgram = true;
            while (continueProgram)
            {
                try
                {
                    Console.Clear();
                    Console.WriteLine("Що ви хочете зробити?");
                    Console.WriteLine("1. Згенерувати QR-код для сід-фрази (шифрування)");
                    Console.WriteLine("2. Розшифрувати QR-код з сід-фразою");
                    Console.WriteLine("3. Вийти з програми");
                    Console.Write("\nВведіть номер опції (1, 2 або 3): ");

                    var choice = Console.ReadLine();
                    switch (choice)
                    {
                        case "1":
                            EncryptAndGenerateQR();
                            break;
                        case "2":
                            DecryptQR();
                            break;
                        case "3":
                            continueProgram = false;
                            Console.WriteLine("\nДякуємо за використання програми!");
                            break;
                        default:
                            Console.WriteLine("\n❌ Невірний вибір. Будь ласка, введіть 1, 2 або 3.");
                            Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                            Console.ReadKey();
                            break;
                    }

                    if (continueProgram && choice is "1" or "2")
                    {
                        Console.WriteLine("\nБажаєте виконати ще одну операцію? (y/n)");
                        var answer = Console.ReadLine()?.ToUpperInvariant();
                        continueProgram = answer is "Y" or "YES";
                        
                        if (!continueProgram)
                        {
                            Console.WriteLine("\nДякуємо за використання програми!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                    Console.ReadKey();
                }
            }
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Need to catch all exceptions to provide user-friendly error messages")]
        private static void EncryptAndGenerateQR()
        {
            string? seedPhrase = null;
            while (seedPhrase == null)
            {
                try
                {
                    Console.WriteLine("\nВимоги до сід-фрази:");
                    Console.WriteLine("- Повинна складатися з 12 або 24 слів");
                    Console.WriteLine("- Слова повинні бути розділені одним пробілом");
                    Console.WriteLine("- Тільки слова зі стандартного словника BIP-39");
                    Console.WriteLine("- Всі слова повинні бути в нижньому регістрі");
                    
                    Console.Write("\nВведіть сід-фразу (12 або 24 слова): ");
                    seedPhrase = Console.ReadLine();
                    if (string.IsNullOrEmpty(seedPhrase))
                    {
                        throw new ArgumentException("Сід-фраза не може бути порожньою");
                    }
                    ValidateSeedPhrase(seedPhrase);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    seedPhrase = null;
                }
            }

            string? password = null;
            while (password == null)
            {
                try
                {
                    Console.WriteLine("\nВимоги до пароля:");
                    Console.WriteLine("- Мінімум 12 символів");
                    Console.WriteLine("- Хоча б одна велика літера (A-Z)");
                    Console.WriteLine("- Хоча б одна мала літера (a-z)");
                    Console.WriteLine("- Хоча б одна цифра (0-9)");
                    Console.WriteLine("- Хоча б один спеціальний символ (!@#$%^&* тощо)");
                    
                    Console.Write("\nВведіть пароль для шифрування: ");
                    password = Console.ReadLine();
                    if (string.IsNullOrEmpty(password))
                    {
                        throw new ArgumentException("Пароль не може бути порожнім");
                    }
                    ValidatePasswordFormat(password);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    password = null;
                }
            }

            string qrFilePath = "seed_qr.png";
            bool validPath = false;
            while (!validPath)
            {
                try
                {
                    Console.Write($"\nВведіть назву файлу для QR-коду (Enter для '{qrFilePath}'): ");
                    string? input = Console.ReadLine()?.Trim();
                    
                    if (!string.IsNullOrEmpty(input))
                    {
                        qrFilePath = input;
                        if (!qrFilePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            qrFilePath += ".png";
                        }
                    }

                    // Перевіряємо, чи можемо створити файл
                    using (File.Create(qrFilePath)) { }
                    File.Delete(qrFilePath);
                    validPath = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Помилка з файлом: {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
            }

            try
            {
                string encryptedSeed = SeedEncryptor.Encrypt(seedPhrase, password);
                Console.WriteLine($"\n🔐 Зашифрована сід-фраза: {encryptedSeed}");

                QRCodeGeneratorUtil.GenerateQRCode(encryptedSeed, qrFilePath);
                Console.WriteLine($"✅ QR-код збережено у {qrFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Помилка при створенні QR-коду: {ex.Message}");
                throw;
            }
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Need to catch all exceptions to provide user-friendly error messages")]
        private static void DecryptQR()
        {
            string? qrFilePath = null;
            while (qrFilePath == null)
            {
                try
                {
                    Console.Write("\nВведіть шлях до QR-коду: ");
                    qrFilePath = Console.ReadLine()?.Trim();
                    
                    if (string.IsNullOrEmpty(qrFilePath))
                    {
                        throw new ArgumentException("Шлях до файлу не може бути порожнім");
                    }

                    if (!File.Exists(qrFilePath))
                    {
                        throw new FileNotFoundException("Файл не знайдено", qrFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    qrFilePath = null;
                }
            }

            string? encryptedSeed = null;
            while (encryptedSeed == null)
            {
                try
                {
                    encryptedSeed = QRCodeReaderUtil.ReadQRCode(qrFilePath);
                    if (string.IsNullOrEmpty(encryptedSeed))
                    {
                        throw new InvalidOperationException("Не вдалося прочитати QR-код");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Бажаєте спробувати інший файл? (y/n)");
                    var retry = Console.ReadLine()?.ToUpperInvariant();
                    if (retry is not ("Y" or "YES"))
                    {
                        throw;
                    }
                    qrFilePath = null;
                    while (qrFilePath == null)
                    {
                        try
                        {
                            Console.Write("\nВведіть шлях до QR-коду: ");
                            qrFilePath = Console.ReadLine()?.Trim();
                            
                            if (string.IsNullOrEmpty(qrFilePath))
                            {
                                throw new ArgumentException("Шлях до файлу не може бути порожнім");
                            }

                            if (!File.Exists(qrFilePath))
                            {
                                throw new FileNotFoundException("Файл не знайдено", qrFilePath);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"\n❌ {innerEx.Message}");
                            Console.WriteLine("Спробуйте ще раз.");
                            qrFilePath = null;
                        }
                    }
                }
            }

            string? password = null;
            while (password == null)
            {
                try
                {
                    Console.Write("\nВведіть пароль для розшифрування: ");
                    password = Console.ReadLine();
                    
                    if (string.IsNullOrEmpty(password))
                    {
                        throw new ArgumentException("Пароль не може бути порожнім");
                    }

                    ValidatePasswordFormat(password);
                    string decryptedSeed = SeedEncryptor.Decrypt(encryptedSeed, password);
                    Console.WriteLine($"\n✅ Розшифрована сід-фраза: {decryptedSeed}");
                }
                catch (CryptographicException)
                {
                    Console.WriteLine("\n❌ Невірний пароль");
                    Console.WriteLine("Спробуйте ще раз.");
                    password = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    password = null;
                }
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

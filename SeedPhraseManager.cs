using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace QrCodeGenerator
{
    internal static class SeedPhraseManager
    {
        public static void ValidateSeedPhrase(string seedPhrase)
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

        public static void ValidatePasswordFormat(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Пароль не може бути порожнім");

            var errors = new System.Collections.Generic.List<string>();

            if (password.Length < 12)
                errors.Add("Пароль закороткий (потрібно мінімум 12 символів)");

            if (!password.Any(c => char.IsUpper(c)))
                errors.Add("Пароль повинен містити хоча б одну велику літеру");

            if (!password.Any(c => char.IsLower(c)))
                errors.Add("Пароль повинен містити хоча б одну малу літеру");

            if (!password.Any(c => char.IsDigit(c)))
                errors.Add("Пароль повинен містити хоча б одну цифру");

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                errors.Add("Пароль повинен містити хоча б один спеціальний символ");

            if (errors.Count > 0)
                throw new ArgumentException(string.Join("\n", errors));
        }

        public static string GetValidQrFilePath(string defaultPath = "seed_qr.png")
        {
            string qrFilePath = defaultPath;
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
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"\n❌ Немає доступу до файлу: {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"\n❌ Помилка доступу до файлу: {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Неочікувана помилка: {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    throw;
                }
            }

            return qrFilePath;
        }

        public static string ReadSeedPhrase()
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

            return seedPhrase;
        }

        public static string ReadPassword()
        {
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
                    
                    Console.Write("\nВведіть пароль: ");
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

            return password;
        }
    }
} 
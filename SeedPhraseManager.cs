using System;
using System.IO;

namespace QrCodeGenerator
{
    internal static class SeedPhraseManager
    {
        private delegate void SecretValidator(ReadOnlySpan<char> value);

        public static void ValidateSeedPhrase(ReadOnlySpan<char> seedPhrase)
        {
            if (seedPhrase.IsEmpty)
            {
                throw new ArgumentException("Сід-фраза не може бути порожньою");
            }

            if (seedPhrase[0] == ' ' || seedPhrase[^1] == ' ' || seedPhrase.Contains("  ", StringComparison.Ordinal))
            {
                throw new ArgumentException("Сід-фраза не повинна містити пробіли на початку/в кінці або подвійні пробіли");
            }

            int wordCount = 0;
            bool allWordsValid = true;
            foreach (Range word in seedPhrase.Split(' '))
            {
                wordCount++;
                allWordsValid &= Bip39Words.IsValidWord(seedPhrase[word]);
            }

            if (wordCount != 12 && wordCount != 24)
            {
                throw new ArgumentException("Сід-фраза повинна містити 12 або 24 слова");
            }

            if (!allWordsValid)
            {
                throw new ArgumentException("Сід-фраза містить недійсні слова. Використовуйте тільки слова зі стандартного списку BIP-39");
            }
        }

        public static void ValidatePasswordFormat(ReadOnlySpan<char> password)
        {
            if (password.IsEmpty)
                throw new ArgumentException("Пароль не може бути порожнім");

            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                hasUpper |= char.IsUpper(c);
                hasLower |= char.IsLower(c);
                hasDigit |= char.IsDigit(c);
                hasSpecial |= !char.IsLetterOrDigit(c);
            }

            var errors = new System.Collections.Generic.List<string>();

            if (password.Length < 12)
                errors.Add("Пароль закороткий (потрібно мінімум 12 символів)");

            if (!hasUpper)
                errors.Add("Пароль повинен містити хоча б одну велику літеру");

            if (!hasLower)
                errors.Add("Пароль повинен містити хоча б одну малу літеру");

            if (!hasDigit)
                errors.Add("Пароль повинен містити хоча б одну цифру");

            if (!hasSpecial)
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

        /// <summary>Reads a masked, validated seed phrase. The caller owns and disposes the buffer.</summary>
        public static SecretBuffer ReadSeedPhrase()
        {
            Console.WriteLine("\nВимоги до сід-фрази:");
            Console.WriteLine("- Повинна складатися з 12 або 24 слів");
            Console.WriteLine("- Слова повинні бути розділені одним пробілом");
            Console.WriteLine("- Тільки слова зі стандартного словника BIP-39");
            Console.WriteLine("- Всі слова повинні бути в нижньому регістрі");

            return ReadValidatedSecret("\nВведіть сід-фразу (12 або 24 слова): ", "Сід-фраза не може бути порожньою", ValidateSeedPhrase);
        }

        /// <summary>Reads a masked, validated password. The caller owns and disposes the buffer.</summary>
        public static SecretBuffer ReadPassword()
        {
            Console.WriteLine("\nВимоги до пароля:");
            Console.WriteLine("- Мінімум 12 символів");
            Console.WriteLine("- Хоча б одна велика літера (A-Z)");
            Console.WriteLine("- Хоча б одна мала літера (a-z)");
            Console.WriteLine("- Хоча б одна цифра (0-9)");
            Console.WriteLine("- Хоча б один спеціальний символ (!@#$%^&* тощо)");

            return ReadValidatedSecret("\nВведіть пароль: ", "Пароль не може бути порожнім", ValidatePasswordFormat);
        }

        private static SecretBuffer ReadValidatedSecret(string prompt, string emptyMessage, SecretValidator validate)
        {
            while (true)
            {
                SecretBuffer secret = ConsoleHelper.ReadMaskedLine(prompt);
                try
                {
                    if (secret.IsEmpty)
                    {
                        throw new ArgumentException(emptyMessage);
                    }

                    validate(secret.Span);
                    return secret;
                }
                catch (ArgumentException ex)
                {
                    secret.Dispose();
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
                catch
                {
                    secret.Dispose();
                    throw;
                }
            }
        }
    }
}

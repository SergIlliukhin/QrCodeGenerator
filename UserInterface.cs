using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace QrCodeGenerator
{
    internal static class UserInterface
    {
        public static void Run()
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
                    Console.WriteLine("3. Розшифрувати текст з сід-фразою");
                    Console.WriteLine("4. Вийти з програми");
                    Console.Write("\nВведіть номер опції (1, 2, 3 або 4): ");

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
                            DecryptText();
                            break;
                        case "4":
                            continueProgram = false;
                            break;
                        default:
                            Console.WriteLine("\n❌ Невірний вибір. Будь ласка, введіть 1, 2, 3 або 4.");
                            Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                            Console.ReadKey();
                            break;
                    }

                    if (continueProgram && choice is "1" or "2" or "3")
                    {
                        Console.WriteLine("\nБажаєте виконати ще одну операцію? (y/n)");
                        var answer = Console.ReadLine()?.ToUpperInvariant();
                        continueProgram = answer is "Y" or "YES";
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ Помилка валідації: {ex.Message}");
                    Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                    Console.ReadKey();
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"\n❌ Помилка доступу до файлу: {ex.Message}");
                    Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                    Console.ReadKey();
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"\n❌ Немає доступу до файлу: {ex.Message}");
                    Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                    Console.ReadKey();
                }
                catch (CryptographicException ex)
                {
                    Console.WriteLine($"\n❌ Помилка шифрування: {ex.Message}");
                    Console.WriteLine("Натисніть будь-яку клавішу, щоб продовжити...");
                    Console.ReadKey();
                }
                // Any other exception propagates to Program.Main, which wipes the screen before reporting it.
            }
        }

        private static void EncryptAndGenerateQR()
        {
            using SecretBuffer seedPhrase = SeedPhraseManager.ReadSeedPhrase();
            using SecretBuffer password = SeedPhraseManager.ReadPassword();
            string qrFilePath = SeedPhraseManager.GetValidQrFilePath();

            string encryptedSeed = SeedEncryptor.Encrypt(seedPhrase.Span, password.Span);
            seedPhrase.Clear();
            password.Clear();

            QRCodeGeneratorUtil.GenerateQRCode(encryptedSeed, qrFilePath);
            Console.WriteLine($"\n✅ QR-код збережено у {qrFilePath}");

            if (ConsoleHelper.AskYesNo("Показати зашифрований текст на екрані? (y/n)")
                && ConsoleHelper.RevealSecret("\n🔐 Зашифрована сід-фраза: ", encryptedSeed))
            {
                ConsoleHelper.AcknowledgeAndWipe();
            }
        }

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
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    qrFilePath = null;
                }
                catch (FileNotFoundException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    qrFilePath = null;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"\n❌ Помилка доступу до файлу: {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    qrFilePath = null;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"\n❌ Немає доступу до файлу: {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    qrFilePath = null;
                }
            }

            string? encryptedSeed = null;
            while (encryptedSeed == null)
            {
                try
                {
                    if (qrFilePath == null)
                    {
                        throw new InvalidOperationException("Шлях до файлу не може бути порожнім");
                    }

                    encryptedSeed = QRCodeReaderUtil.ReadQRCode(qrFilePath);
                    if (string.IsNullOrEmpty(encryptedSeed))
                    {
                        throw new InvalidOperationException("Не вдалося прочитати QR-код");
                    }
                }
                catch (InvalidOperationException ex)
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
                        catch (ArgumentException innerEx)
                        {
                            Console.WriteLine($"\n❌ {innerEx.Message}");
                            Console.WriteLine("Спробуйте ще раз.");
                            qrFilePath = null;
                        }
                        catch (FileNotFoundException innerEx)
                        {
                            Console.WriteLine($"\n❌ {innerEx.Message}");
                            Console.WriteLine("Спробуйте ще раз.");
                            qrFilePath = null;
                        }
                        catch (IOException innerEx)
                        {
                            Console.WriteLine($"\n❌ Помилка доступу до файлу: {innerEx.Message}");
                            Console.WriteLine("Спробуйте ще раз.");
                            qrFilePath = null;
                        }
                        catch (UnauthorizedAccessException innerEx)
                        {
                            Console.WriteLine($"\n❌ Немає доступу до файлу: {innerEx.Message}");
                            Console.WriteLine("Спробуйте ще раз.");
                            qrFilePath = null;
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"\n❌ Помилка читання файлу: {ex.Message}");
                    Console.WriteLine("Бажаєте спробувати інший файл? (y/n)");
                    var retry = Console.ReadLine()?.ToUpperInvariant();
                    if (retry is not ("Y" or "YES"))
                    {
                        throw;
                    }
                    qrFilePath = null;
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"\n❌ Немає доступу до файлу: {ex.Message}");
                    Console.WriteLine("Бажаєте спробувати інший файл? (y/n)");
                    var retry = Console.ReadLine()?.ToUpperInvariant();
                    if (retry is not ("Y" or "YES"))
                    {
                        throw;
                    }
                    qrFilePath = null;
                }
            }

            DecryptWithPassword(encryptedSeed);
        }

        private static void DecryptText()
        {
            string? encryptedText = null;
            while (encryptedText == null)
            {
                try
                {
                    Console.WriteLine("\nВведіть зашифрований текст:");
                    encryptedText = Console.ReadLine();
                    if (string.IsNullOrEmpty(encryptedText))
                    {
                        throw new ArgumentException("Зашифрований текст не може бути порожнім");
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                    encryptedText = null;
                }
            }

            DecryptWithPassword(encryptedText);
        }

        private static void DecryptWithPassword(string encryptedText)
        {
            while (true)
            {
                try
                {
                    using SecretBuffer password = SeedPhraseManager.ReadPassword();
                    using SecretBuffer decryptedSeed = SeedEncryptor.Decrypt(encryptedText, password.Span);
                    password.Clear();
                    ShowDecryptedSeedPhrase(decryptedSeed);
                    return;
                }
                catch (CryptographicException)
                {
                    Console.WriteLine("\n❌ Невірний пароль");
                    Console.WriteLine("Спробуйте ще раз.");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n❌ {ex.Message}");
                    Console.WriteLine("Спробуйте ще раз.");
                }
            }
        }

        private static void ShowDecryptedSeedPhrase(SecretBuffer decryptedSeed)
        {
            Console.WriteLine("\n✅ Сід-фразу успішно розшифровано.");

            if (ConsoleHelper.AskYesNo("⚠️ Показувати сід-фразу на екрані небезпечно. Ви впевнені, що хочете її побачити? (y/n)"))
            {
                if (ConsoleHelper.RevealSecret("\n🔓 Сід-фраза: ", decryptedSeed.Span))
                {
                    ConsoleHelper.AcknowledgeAndWipe();
                }

                return;
            }

            Console.WriteLine("Сід-фразу приховано.");
        }
    }
}

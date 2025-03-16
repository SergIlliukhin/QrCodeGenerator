using System;
using System.Text.RegularExpressions;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            string seedPhrase;
            bool isSeedValid = false;
            do
            {
                Console.WriteLine("Вимоги до сід-фрази:");
                Console.WriteLine("- Повинна складатися з 12 або 24 слів");
                Console.WriteLine("- Слова повинні бути розділені одним пробілом");
                Console.WriteLine("- Тільки слова зі стандартного словника BIP-39");
                Console.WriteLine("- Всі слова повинні бути в нижньому регістрі");
                
                Console.Write("\nВведіть сід-фразу: ");
                seedPhrase = Console.ReadLine();

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
                password = Console.ReadLine();

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
            string qrFilePath = Console.ReadLine();
            
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
        catch (ArgumentException ex)
        {
            Console.WriteLine($"\n❌ Помилка валідації: {ex.Message}");
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            Console.WriteLine($"\n❌ Помилка шифрування: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Неочікувана помилка: {ex.Message}");
        }
    }

    private static void ValidateSeedPhrase(string seedPhrase)
    {
        if (string.IsNullOrEmpty(seedPhrase))
            throw new ArgumentException("Сід-фраза не може бути порожньою");

        // Перевірка на зайві пробіли
        if (seedPhrase.StartsWith(" ") || seedPhrase.EndsWith(" ") || seedPhrase.Contains("  "))
            throw new ArgumentException("Сід-фраза не повинна містити зайві пробіли");

        // Розділення на слова
        var words = seedPhrase.Split(' ');

        // Перевірка кількості слів
        if (words.Length != 12 && words.Length != 24)
            throw new ArgumentException($"Сід-фраза повинна складатися з 12 або 24 слів (зараз {words.Length})");

        // Перевірка кожного слова
        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                throw new ArgumentException("Знайдено порожнє слово");

            if (!Regex.IsMatch(word, "^[a-z]+$"))
                throw new ArgumentException($"Слово '{word}' містить неприпустимі символи (дозволені тільки малі літери)");

            if (!Bip39Words.IsValidWord(word))
                throw new ArgumentException($"Слово '{word}' відсутнє в словнику BIP-39");
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

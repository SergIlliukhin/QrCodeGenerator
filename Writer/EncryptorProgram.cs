using System;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        try
        {
            Console.Write("Введіть сід-фразу: ");
            string seedPhrase = Console.ReadLine();

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

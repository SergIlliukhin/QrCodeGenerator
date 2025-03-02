using System;

class Program
{
    static void Main()
    {
        Console.Write("Введіть сід-фразу: ");
        string seedPhrase = Console.ReadLine();

        Console.Write("Введіть секретний пароль: ");
        string password = Console.ReadLine();

        Console.Write("Введіть назву файлу для QR-коду (або натисніть Enter для значення за замовчуванням 'seed_qr.png'): ");
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
        Console.WriteLine($"🔐 Зашифрована сід-фраза: {encryptedSeed}");

        QRCodeGeneratorUtil.GenerateQRCode(encryptedSeed, qrFilePath);
        Console.WriteLine($"✅ QR-код збережено у {qrFilePath}");
    }
}

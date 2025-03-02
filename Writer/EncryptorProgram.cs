using System;

class Program
{
    static void Main()
    {
        Console.Write("Введіть сід-фразу: ");
        string seedPhrase = Console.ReadLine();

        Console.Write("Введіть секретний пароль: ");
        string password = Console.ReadLine();

        string encryptedSeed = SeedEncryptor.Encrypt(seedPhrase, password);
        Console.WriteLine($"🔐 Зашифрована сід-фраза: {encryptedSeed}");

        string qrFilePath = "seed_qr.png";
        QRCodeGeneratorUtil.GenerateQRCode(encryptedSeed, qrFilePath);
        Console.WriteLine($"✅ QR-код збережено у {qrFilePath}");
    }
}

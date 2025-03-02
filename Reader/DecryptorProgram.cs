using System;

class DecryptorProgram
{
    static void Main()
    {
        Console.Write("Введіть шлях до QR-коду: ");
        string qrFilePath = Console.ReadLine();

        Console.Write("Введіть секретний пароль: ");
        string password = Console.ReadLine();

        string encryptedSeed = QRCodeReaderUtil.ReadQRCode(qrFilePath);
        Console.WriteLine($"📄 Зашифровані дані: {encryptedSeed}");

        string decryptedSeed = SeedEncryptor.Decrypt(encryptedSeed, password);
        Console.WriteLine($"✅ Відновлена сід-фраза: {decryptedSeed}");
    }
}

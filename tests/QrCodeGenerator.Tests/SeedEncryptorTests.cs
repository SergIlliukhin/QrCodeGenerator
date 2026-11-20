using System.Security.Cryptography;
using System.Text;

namespace QrCodeGenerator.Tests;

public class SeedEncryptorTests
{
    private const string Password = "Sergii19_Update";

    // Example vectors published in README.md ("Тестові дані"); they pin ciphertext format v1.
    public static TheoryData<string, string, string> ReadmeVectors => new()
    {
        {
            "AR+rtCoeci9esJYuGG7fQuoQD080Wg6J6fj3ZFWje291ICUS8x2sj56nVTAe10SIcaQR8MBJX33G0v4bMT8dMBxRdLtKNgJrL7k2lojVnbGzHnxpAlA+f5hCC22OUHM4YMtkXWlXXRoV68Edb4Ct3rhBB8z6i/BYznqdvIlcF8ZprHqD1qTj7u3vE2MI3U0pSw==",
            "Sergii19_Update",
            "glory promote mansion submit captain finger virus verb utility general welcome flush"
        },
        {
            "AY48ARdedeMSTIRP4ZAob+VuPl25VjJkSjI+EKY7QTz5ABNVfY+LZX+fL1krRU+f2ote3SjlQSll7qRMtTe36x1pLg5wl11orD1wvsD3LJCMHD1m43reVNqTWfrVtnO3Rgqufe00secPE6eK527PgSvSwpTOo2z+WIx6MelR4dfu0ZgIPhYcQwZzDKg2lXCNS0tS5x1w/NIa95TjIPPntjLBhafrXd0OIxsdLH7GJugCZX0tXuKZaOQhgMcY6QDilXRWcBViPMxifTAWRXyMuISGKqP/yjlpr8f3jEha9xCC",
            "Mariaaaa)$34",
            "future alarm discover sustain maple keen shrimp uncle wagon tray puzzle ring embrace velvet elegant robust census provide layer decorate cause foam pride timber"
        },
    };

    [Theory]
    [MemberData(nameof(ReadmeVectors))]
    public void DecryptsReadmeVectors(string ciphertext, string password, string phrase)
    {
        using SecretBuffer result = SeedEncryptor.Decrypt(ciphertext, password);
        Assert.Equal(phrase, result.Span.ToString());
    }

    [Fact]
    public void RoundTripsSeedPhrase()
    {
        const string phrase = "glory promote mansion submit captain finger virus verb utility general welcome flush";
        string ciphertext = SeedEncryptor.Encrypt(phrase, Password);

        using SecretBuffer result = SeedEncryptor.Decrypt(ciphertext, Password);
        Assert.Equal(phrase, result.Span.ToString());
    }

    [Fact]
    public void WrongPasswordThrows()
    {
        string ciphertext = SeedEncryptor.Encrypt("abandon ability able", Password);
        Assert.Throws<CryptographicException>(() => SeedEncryptor.Decrypt(ciphertext, "Wrong_Pass123!"));
    }

    [Fact]
    public void OversizedCiphertextBodyThrows()
    {
        string ciphertext = EncryptRaw(Encoding.UTF8.GetBytes(new string('a', SeedEncryptor.MAX_CIPHERTEXT_BODY + 100)));
        Assert.Throws<CryptographicException>(() => SeedEncryptor.Decrypt(ciphertext, Password));
    }

    [Fact]
    public void PlaintextLongerThanBufferThrowsInsteadOfTruncating()
    {
        // Fits the byte bound but exceeds the 1024-char buffer.
        string ciphertext = EncryptRaw(Encoding.UTF8.GetBytes(new string('a', SecretBuffer.DefaultCapacity + 1)));
        Assert.Throws<CryptographicException>(() => SeedEncryptor.Decrypt(ciphertext, Password));
    }

    [Fact]
    public void InvalidUtf8PlaintextThrows()
    {
        string ciphertext = EncryptRaw([0x61, 0xFF, 0xFE, 0x62]);
        Assert.Throws<CryptographicException>(() => SeedEncryptor.Decrypt(ciphertext, Password));
    }

    [Theory]
    [InlineData("Sergii19_Update")]
    [InlineData("Mariaaaa)$34")]
    public void ExampleGoodPasswordsPassBothValidators(string password)
    {
        SeedEncryptor.ValidatePassword(password);
        SeedPhraseManager.ValidatePasswordFormat(password);
    }

    [Theory]
    [InlineData("sergii19_update")] // no upper
    [InlineData("SERGII19_UPDATE")] // no lower
    [InlineData("Sergii_Update!")]  // no digit
    [InlineData("Sergii19Update")]  // no special
    [InlineData("Sergii19_Up")]     // < 12 chars
    [InlineData("")]
    public void WeakPasswordsFailBothValidators(string password)
    {
        Assert.Throws<ArgumentException>(() => SeedEncryptor.ValidatePassword(password));
        Assert.Throws<ArgumentException>(() => SeedPhraseManager.ValidatePasswordFormat(password));
    }

    [Theory]
    [InlineData("Пароль1234_ok")] // Cyrillic: ASCII upper missing in encryptor rules
    public void EncryptorKeepsAsciiClassRules(string password)
    {
        Assert.Throws<ArgumentException>(() => SeedEncryptor.ValidatePassword(password));
    }

    /// <summary>Builds a v1 ciphertext over arbitrary plaintext bytes, bypassing Encrypt's input checks.</summary>
    private static string EncryptRaw(byte[] plaintext)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(32);
        byte[] iv = RandomNumberGenerator.GetBytes(16);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(Password, salt, 600000, HashAlgorithmName.SHA256, 32);
        using var aes = Aes.Create();
        aes.Key = key;
        byte[] cipher = aes.EncryptCbc(plaintext, iv, PaddingMode.PKCS7);
        return Convert.ToBase64String([1, .. salt, .. iv, .. cipher]);
    }
}

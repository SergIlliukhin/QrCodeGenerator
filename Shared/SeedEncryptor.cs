using System;
using System.Security.Cryptography;
using System.Text;

namespace QrCodeGenerator
{
    internal static class SeedEncryptor
    {
        private const int SALT_SIZE = 32;
        private const int KEY_SIZE = 32; // 256 bits
        private const int IV_SIZE = 16; // 128 bits
        private const int BLOCK_SIZE = 16;
        private const int HEADER_SIZE = 1 + SALT_SIZE + IV_SIZE;
        private const int ITERATIONS = 600000;
        private const int MAX_PLAINTEXT_LENGTH = SecretBuffer.DefaultCapacity; // Максимальна довжина вхідного тексту (символів)
        // UTF-8 uses at most 4 bytes per char here, plus at most one PKCS7 padding block.
        internal const int MAX_CIPHERTEXT_BODY = MAX_PLAINTEXT_LENGTH * 4 + BLOCK_SIZE;
        private const byte ENCRYPTION_VERSION = 1; // Версія формату шифрування

        private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public static string Encrypt(ReadOnlySpan<char> seedPhrase, ReadOnlySpan<char> password)
        {
            if (seedPhrase.IsEmpty)
                throw new ArgumentException("Сід-фраза не може бути порожньою");

            if (seedPhrase.Length > MAX_PLAINTEXT_LENGTH)
                throw new ArgumentException($"Сід-фраза занадто довга (максимум {MAX_PLAINTEXT_LENGTH} символів)");

            ValidatePassword(password);

            byte[] key = GC.AllocateArray<byte>(KEY_SIZE, pinned: true);
            byte[] plain = GC.AllocateArray<byte>(StrictUtf8.GetMaxByteCount(seedPhrase.Length), pinned: true);
            try
            {
                byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
                byte[] iv = RandomNumberGenerator.GetBytes(IV_SIZE);
                Rfc2898DeriveBytes.Pbkdf2(password, salt, key, ITERATIONS, HashAlgorithmName.SHA256);
                int plainLength = StrictUtf8.GetBytes(seedPhrase, plain);

                using var aes = Aes.Create();
                aes.SetKey(key);
                byte[] cipher = aes.EncryptCbc(plain.AsSpan(0, plainLength), iv, PaddingMode.PKCS7);

                byte[] output = new byte[HEADER_SIZE + cipher.Length];
                output[0] = ENCRYPTION_VERSION;
                salt.CopyTo(output, 1);
                iv.CopyTo(output, 1 + SALT_SIZE);
                cipher.CopyTo(output, HEADER_SIZE);
                return Convert.ToBase64String(output);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Помилка шифрування", ex);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plain);
            }
        }

        /// <summary>Decrypts into a new <see cref="SecretBuffer"/> owned (and disposed) by the caller.</summary>
        public static SecretBuffer Decrypt(string encryptedText, ReadOnlySpan<char> password)
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new ArgumentException("Зашифрований текст не може бути порожнім");

            ValidatePassword(password);

            byte[] encryptedData;
            try
            {
                encryptedData = Convert.FromBase64String(encryptedText);
            }
            catch (FormatException)
            {
                throw new CryptographicException("Невірний формат зашифрованих даних");
            }

            if (encryptedData.Length < HEADER_SIZE)
                throw new CryptographicException("Невірний формат зашифрованих даних");

            byte version = encryptedData[0];
            if (version != ENCRYPTION_VERSION)
                throw new CryptographicException($"Непідтримувана версія шифрування: {version}");

            ReadOnlySpan<byte> salt = encryptedData.AsSpan(1, SALT_SIZE);
            ReadOnlySpan<byte> iv = encryptedData.AsSpan(1 + SALT_SIZE, IV_SIZE);
            ReadOnlySpan<byte> body = encryptedData.AsSpan(HEADER_SIZE);

            if (body.Length > MAX_CIPHERTEXT_BODY)
                throw new CryptographicException("Невірний формат зашифрованих даних");

            byte[] key = GC.AllocateArray<byte>(KEY_SIZE, pinned: true);
            byte[] plain = GC.AllocateArray<byte>(Math.Max(body.Length, BLOCK_SIZE), pinned: true);
            try
            {
                Rfc2898DeriveBytes.Pbkdf2(password, salt, key, ITERATIONS, HashAlgorithmName.SHA256);

                using var aes = Aes.Create();
                aes.SetKey(key);
                int plainLength = aes.DecryptCbc(body, iv, plain, PaddingMode.PKCS7);
                ReadOnlySpan<byte> plainBytes = plain.AsSpan(0, plainLength);

                int charCount;
                try
                {
                    charCount = StrictUtf8.GetCharCount(plainBytes);
                }
                catch (DecoderFallbackException)
                {
                    throw new CryptographicException("Помилка розшифрування");
                }

                if (charCount == 0)
                    throw new CryptographicException("Помилка розшифрування: отримано порожній результат");

                if (charCount > MAX_PLAINTEXT_LENGTH)
                    throw new CryptographicException("Помилка розшифрування");

                var result = new SecretBuffer(MAX_PLAINTEXT_LENGTH);
                try
                {
                    result.SetLength(StrictUtf8.GetChars(plainBytes, result.ResetForWrite()));
                    return result;
                }
                catch
                {
                    result.Dispose();
                    throw;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plain);
            }
        }

        internal static void ValidatePassword(ReadOnlySpan<char> password)
        {
            if (password.IsEmpty)
                throw new ArgumentException("Пароль не може бути порожнім");

            if (password.Length < 12)
                throw new ArgumentException("Пароль повинен містити мінімум 12 символів");

            // Перевірка на наявність різних типів символів (ASCII-класи, як і раніше)
            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;
            foreach (char c in password)
            {
                if (char.IsAsciiLetterUpper(c)) hasUpper = true;
                else if (char.IsAsciiLetterLower(c)) hasLower = true;
                else if (char.IsAsciiDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
                throw new ArgumentException("Пароль повинен містити великі та малі літери, цифри та спеціальні символи");
        }
    }
}

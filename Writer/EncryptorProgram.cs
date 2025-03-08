using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using QrCodeGenerator.Shared;
using System.Runtime.CompilerServices;
using System.Text;

namespace QrCodeGenerator.Writer
{
    internal sealed class Program
    {
        private static readonly CompositeFormat ValidationErrorFormat = CompositeFormat.Parse(Resources.Messages.ValidationError);
        private static readonly CompositeFormat EncryptedSeedPhraseFormat = CompositeFormat.Parse(Resources.Messages.EncryptedSeedPhrase);
        private static readonly CompositeFormat QrCodeSavedFormat = CompositeFormat.Parse(Resources.Messages.QrCodeSaved);
        private static readonly CompositeFormat EncryptionErrorFormat = CompositeFormat.Parse(Resources.Messages.EncryptionError);
        private static readonly CompositeFormat InvalidWordCountFormat = CompositeFormat.Parse(Resources.Messages.InvalidWordCount);
        private static readonly CompositeFormat InvalidWordCharactersFormat = CompositeFormat.Parse(Resources.Messages.InvalidWordCharacters);
        private static readonly CompositeFormat WordNotInDictionaryFormat = CompositeFormat.Parse(Resources.Messages.WordNotInDictionary);
        
        // Simple string formats
        private static readonly CompositeFormat SimpleFormat = CompositeFormat.Parse("{0}");

        static void Main()
    {
        try
        {
            string seedPhrase;
            bool isSeedValid = false;
            do
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.SeedPhraseRequirements));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.SeedPhraseLength));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.SeedPhraseSpacing));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.SeedPhraseDictionary));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.SeedPhraseLowercase));
                
                Console.Write(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.EnterSeedPhrase));
                seedPhrase = Console.ReadLine() ?? string.Empty;

                try
                {
                    ValidateSeedPhrase(seedPhrase);
                    isSeedValid = true;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, ex.Message));
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.TryAgain));
                }
            } while (!isSeedValid);

            string password;
            bool isPasswordValid = false;
            do
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.PasswordRequirements));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.PasswordLength));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.PasswordUppercase));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.PasswordLowercase));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.PasswordDigit));
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.PasswordSpecial));
                
                Console.Write(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.EnterPassword));
                password = Console.ReadLine() ?? string.Empty;

                try
                {
                    ValidatePasswordFormat(password);
                    isPasswordValid = true;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, ex.Message));
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.TryAgain));
                }
            } while (!isPasswordValid);

            Console.Write(string.Format(CultureInfo.InvariantCulture, SimpleFormat, Resources.Messages.EnterQrFileName));
            string qrFilePath = Console.ReadLine() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(qrFilePath))
            {
                qrFilePath = Resources.Messages.DefaultQrFileName;
            }
            else if (!qrFilePath.EndsWith(Resources.Messages.PngExtension, StringComparison.OrdinalIgnoreCase))
            {
                qrFilePath += Resources.Messages.PngExtension;
            }

            string encryptedSeed = SeedEncryptor.Encrypt(seedPhrase, password);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, EncryptedSeedPhraseFormat, encryptedSeed));

            QRCodeGeneratorUtil.GenerateQRCode(encryptedSeed, qrFilePath);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, QrCodeSavedFormat, qrFilePath));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, ex.Message));
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, EncryptionErrorFormat, ex.Message));
        }
        catch (System.IO.IOException ex)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, ValidationErrorFormat, ex.Message));
        }
    }

    private static void ValidateSeedPhrase(string seedPhrase)
    {
        if (string.IsNullOrEmpty(seedPhrase))
            throw new ArgumentException(Resources.Messages.EmptySeedPhrase);

        // Перевірка на зайві пробіли
        if (seedPhrase.StartsWith(' ') || seedPhrase.EndsWith(' ') || seedPhrase.Contains(Resources.Messages.MultipleSpacesPattern, StringComparison.Ordinal))
            throw new ArgumentException(Resources.Messages.ExtraSpaces);

        // Розділення на слова
        var words = seedPhrase.Split(' ');

        // Перевірка кількості слів
        if (words.Length != 12 && words.Length != 24)
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, InvalidWordCountFormat, words.Length));

        // Перевірка кожного слова
        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                throw new ArgumentException(Resources.Messages.EmptyWord);

            if (!Regex.IsMatch(word, Resources.Messages.LowercaseLettersPattern, RegexOptions.CultureInvariant))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, InvalidWordCharactersFormat, word));

            if (!Bip39Words.IsValidWord(word))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, WordNotInDictionaryFormat, word));
        }
    }

    private static void ValidatePasswordFormat(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException(Resources.Messages.EmptyPassword);

        var errors = new System.Collections.Generic.List<string>();

        if (password.Length < 12)
            errors.Add(Resources.Messages.PasswordTooShort);

        if (!Regex.IsMatch(password, Resources.Messages.UppercaseLetterPattern, RegexOptions.CultureInvariant))
            errors.Add(Resources.Messages.NoUppercase);

        if (!Regex.IsMatch(password, Resources.Messages.LowercaseLetterPattern, RegexOptions.CultureInvariant))
            errors.Add(Resources.Messages.NoLowercase);

        if (!Regex.IsMatch(password, Resources.Messages.DigitPattern, RegexOptions.CultureInvariant))
            errors.Add(Resources.Messages.NoDigit);

        if (!Regex.IsMatch(password, Resources.Messages.SpecialCharPattern, RegexOptions.CultureInvariant))
            errors.Add(Resources.Messages.NoSpecialChar);

        if (errors.Count > 0)
            throw new ArgumentException(string.Join(Resources.Messages.NewLine, errors));
    }
}
}

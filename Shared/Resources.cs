using System;

namespace QrCodeGenerator.Shared
{
    internal static class Resources
    {
        internal static class Messages
        {
            public const string SeedPhraseRequirements = "Вимоги до сід-фрази:";
            public const string SeedPhraseLength = "- Повинна складатися з 12 або 24 слів";
            public const string SeedPhraseSpacing = "- Слова повинні бути розділені одним пробілом";
            public const string SeedPhraseDictionary = "- Тільки слова зі стандартного словника BIP-39";
            public const string SeedPhraseLowercase = "- Всі слова повинні бути в нижньому регістрі";
            public const string EnterSeedPhrase = "\nВведіть сід-фразу: ";
            public const string TryAgain = "Спробуйте ще раз.";
            
            public const string PasswordRequirements = "\nВимоги до пароля:";
            public const string PasswordLength = "- Мінімум 12 символів";
            public const string PasswordUppercase = "- Хоча б одна велика літера (A-Z)";
            public const string PasswordLowercase = "- Хоча б одна мала літера (a-z)";
            public const string PasswordDigit = "- Хоча б одна цифра (0-9)";
            public const string PasswordSpecial = "- Хоча б один спеціальний символ (!@#$%^&* тощо)";
            public const string EnterPassword = "\nВведіть секретний пароль: ";
            
            public const string EnterQrFileName = "\nВведіть назву файлу для QR-коду (або натисніть Enter для значення за замовчуванням 'seed_qr.png'): ";
            public const string DefaultQrFileName = "seed_qr.png";
            public const string EncryptedSeedPhrase = "\n🔐 Зашифрована сід-фраза: {0}";
            public const string QrCodeSaved = "✅ QR-код збережено у {0}";
            
            public const string ValidationError = "\n❌ Помилка валідації: {0}";
            public const string EncryptionError = "\n❌ Помилка шифрування: {0}";
            public const string UnexpectedError = "\n❌ Неочікувана помилка: {0}";
            
            public const string EmptySeedPhrase = "Сід-фраза не може бути порожньою";
            public const string ExtraSpaces = "Сід-фраза не повинна містити зайві пробіли";
            public const string InvalidWordCount = "Сід-фраза повинна складатися з 12 або 24 слів (зараз {0})";
            public const string EmptyWord = "Знайдено порожнє слово";
            public const string InvalidWordCharacters = "Слово '{0}' містить неприпустимі символи (дозволені тільки малі літери)";
            public const string WordNotInDictionary = "Слово '{0}' відсутнє в словнику BIP-39";
            
            public const string EmptyPassword = "Пароль не може бути порожнім";
            public const string PasswordTooShort = "Пароль закороткий (потрібно мінімум 12 символів)";
            public const string NoUppercase = "Пароль повинен містити хоча б одну велику літеру";
            public const string NoLowercase = "Пароль повинен містити хоча б одну малу літеру";
            public const string NoDigit = "Пароль повинен містити хоча б одну цифру";
            public const string NoSpecialChar = "Пароль повинен містити хоча б один спеціальний символ";
            public const string PngExtension = ".png";
            
            // Regex patterns
            public const string MultipleSpacesPattern = "  ";
            public const string LowercaseLettersPattern = "^[a-z]+$";
            public const string UppercaseLetterPattern = "[A-Z]";
            public const string LowercaseLetterPattern = "[a-z]";
            public const string DigitPattern = "[0-9]";
            public const string SpecialCharPattern = "[^A-Za-z0-9]";
            
            // Reader messages
            public const string EnterQrPath = "Введіть шлях до QR-коду: ";
            public const string DecryptedSeedPhrase = "✅ Відновлена сід-фраза: {0}";
            public const string InvalidPassword = "❌ Невірний пароль";
            public const string QrNotFound = "❌ QR-код не знайдено за шляхом: {0}";
            public const string InvalidImageFile = "❌ Неможливо відкрити файл як зображення";
            public const string QrNotRecognized = "❌ QR-код не розпізнано";
            public const string QrReadError = "❌ Помилка при читанні QR-коду";
            
            // Other constants
            public const string NewLine = "\n";
        }
    }
}

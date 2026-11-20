using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace QrCodeGenerator.E2ETests.Infrastructure;

/// <summary>Public example data from README.md ("Тестові дані").</summary>
internal static class TestData
{
    public const string Seed = "glory promote mansion submit captain finger virus verb utility general welcome flush";
    public const string Password = "Sergii19_Update";
    public const string Ciphertext =
        "AR+rtCoeci9esJYuGG7fQuoQD080Wg6J6fj3ZFWje291ICUS8x2sj56nVTAe10SIcaQR8MBJX33G0v4bMT8dMBxRdLtKNgJrL7k2lojVnbGzHnxpAlA+f5hCC22OUHM4YMtkXWlXXRoV68Edb4Ct3rhBB8z6i/BYznqdvIlcF8ZprHqD1qTj7u3vE2MI3U0pSw==";
}

/// <summary>App prompts the scenarios wait on.</summary>
internal static class Ui
{
    public const string Menu = "Введіть номер опції";
    public const string SeedPrompt = "Введіть сід-фразу";
    public const string SeedRules = "Вимоги до сід-фрази";
    public const string PasswordPrompt = "Введіть пароль";
    public const string FilePrompt = "Введіть назву файлу";
    public const string ShowCipher = "Показати зашифрований текст";
    public const string CipherLabel = "Зашифрована сід-фраза:";
    public const string Acknowledge = "щоб приховати";
    public const string Another = "ще одну операцію";
    public const string QrPathPrompt = "шлях до QR-коду";
    public const string RevealConfirm = "Ви впевнені";
    public const string SeedLabel = "Сід-фраза:";
    public const string Hidden = "Сід-фразу приховано";
    public const string WrongPassword = "Невірний пароль";
    public const string CipherTextPrompt = "Введіть зашифрований текст";
    public const string InvalidWords = "недійсні слова";
    public const string Farewell = "Дякуємо";
    public const string UnexpectedError = "Неочікувана помилка";
    public const string OutputRedirected = "вивід перенаправлено";
    public const string InputRedirected = "ввід перенаправлено";
}

internal static class SecretAssert
{
    public static void NoSeedWords(ITerminalSession s, string text, string seed = TestData.Seed)
    {
        foreach (string word in seed.Split(' '))
        {
            if (Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b"))
            {
                throw new XunitException($"[{s.Name}] seed word \"{word}\" found on screen/scrollback:\n{s.Tail(text)}");
            }
        }
    }

    public static void Absent(ITerminalSession s, string text, string needle, string what)
    {
        if (text.Contains(needle, StringComparison.Ordinal))
        {
            throw new XunitException($"[{s.Name}] {what} still present on screen/scrollback:\n{s.Tail(text)}");
        }
    }

    public static void Present(ITerminalSession s, string text, string needle, string what)
    {
        if (!text.Contains(needle, StringComparison.Ordinal))
        {
            throw new XunitException($"[{s.Name}] expected {what} on screen:\n{s.Tail(text)}");
        }
    }

    /// <summary>Counts '*' between the last <paramref name="from"/> marker and the next <paramref name="to"/> marker.</summary>
    public static int MaskCount(string text, string from, string to)
    {
        int start = text.LastIndexOf(from, StringComparison.Ordinal);
        if (start < 0) return -1;
        start += from.Length;
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        string segment = end < 0 ? text[start..] : text[start..end];
        return segment.Count(c => c == '*');
    }

    /// <summary>A run of three or more mask characters (left behind by masked entry).</summary>
    public static void NoMaskRun(ITerminalSession s, string text) => Absent(s, text, "***", "masked-input asterisks");
}

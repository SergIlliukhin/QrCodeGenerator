using System;
using System.Globalization;

namespace QrCodeGenerator
{
    internal static class ConsoleHelper
    {
        public static string ReadNonEmptyLine(string prompt, string errorMessage = "Значення не може бути порожнім")
        {
            while (true)
            {
                Console.Write(prompt);
                var input = Console.ReadLine()?.Trim();
                
                if (!string.IsNullOrEmpty(input))
                {
                    return input;
                }
                
                Console.WriteLine($"\n❌ {errorMessage}");
                Console.WriteLine("Спробуйте ще раз.");
            }
        }

        public static bool AskYesNo(string question)
        {
            Console.WriteLine(question);
            var answer = Console.ReadLine()?.ToUpperInvariant();
            return answer is "Y" or "YES";
        }

        public static void WaitForKey(string message = "Натисніть будь-яку клавішу, щоб продовжити...")
        {
            Console.WriteLine(message);
            Console.ReadKey(true);
        }

        public static void ShowError(string message)
        {
            Console.WriteLine($"\n❌ {message}");
        }

        public static void ShowSuccess(string message)
        {
            Console.WriteLine($"✅ {message}");
        }

        public static void ShowInfo(string message)
        {
            Console.WriteLine($"ℹ️ {message}");
        }

        public static void ClearScreen()
        {
            Console.Clear();
        }
    }
} 
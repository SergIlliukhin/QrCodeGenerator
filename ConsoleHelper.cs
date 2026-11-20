using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QrCodeGenerator
{
    internal static class ConsoleHelper
    {
        private static readonly Lazy<Stream> RawStdout = new(Console.OpenStandardOutput);
        private static volatile bool _secretsTouched;

        /// <summary>True once any secret was typed or shown in this session; exit paths wipe the screen if set.</summary>
        public static bool SecretsTouched => _secretsTouched;

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

        /// <summary>Reads a line without echo (one '*' per char) into a pinned buffer the caller must dispose.</summary>
        public static SecretBuffer ReadMaskedLine(string prompt)
        {
            _secretsTouched = true;
            Console.Write(prompt);
            var input = new SecretBuffer();

            try
            {
                while (true)
                {
                    var keyInfo = Console.ReadKey(intercept: true);

                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        return input;
                    }

                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (input.RemoveLast())
                        {
                            Console.Write("\b \b");
                        }

                        continue;
                    }

                    if (char.IsControl(keyInfo.KeyChar))
                    {
                        continue;
                    }

                    if (input.Append(keyInfo.KeyChar))
                    {
                        Console.Write('*');
                    }
                }
            }
            catch
            {
                input.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Writes <paramref name="label"/> and the secret straight to the stdout stream (bypassing the buffered
        /// <see cref="Console.Out"/> writer). Refuses when stdout is redirected. Returns whether it was shown.
        /// </summary>
        public static bool RevealSecret(string label, ReadOnlySpan<char> secret)
        {
            if (Console.IsOutputRedirected)
            {
                Console.Error.WriteLine("\n❌ Показ секрету можливий лише в інтерактивному терміналі (вивід перенаправлено).");
                return false;
            }

            _secretsTouched = true;
            Console.Write(label);
            Console.Out.Flush();

            byte[] bytes = GC.AllocateArray<byte>(Encoding.UTF8.GetMaxByteCount(secret.Length), pinned: true);
            try
            {
                int length = Encoding.UTF8.GetBytes(secret, bytes);
                RawStdout.Value.Write(bytes, 0, length);
                RawStdout.Value.Flush();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytes);
            }

            Console.WriteLine();
            return true;
        }

        /// <summary>Waits for a key, then wipes screen and scrollback.</summary>
        public static void AcknowledgeAndWipe()
        {
            WaitForKey("\nНатисніть будь-яку клавішу, щоб приховати та очистити екран...");
            WipeScreen();
        }

        /// <summary>Clears the visible screen and, outside Windows, the terminal scrollback (ESC[3J).</summary>
        public static void WipeScreen()
        {
            if (Console.IsOutputRedirected)
            {
                return;
            }

            Console.Out.Flush();
            Console.Clear();
            if (!OperatingSystem.IsWindows())
            {
                RawStdout.Value.Write("\e[3J"u8);
                RawStdout.Value.Flush();
            }
        }

        public static void WipeIfSecretsTouched()
        {
            if (_secretsTouched)
            {
                WipeScreen();
            }
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

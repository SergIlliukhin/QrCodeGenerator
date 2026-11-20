using System;
using System.Diagnostics.CodeAnalysis;

namespace QrCodeGenerator
{
    [SuppressMessage("Design", "CA1852:Seal internal types", Justification = "Program class is implicitly sealed")]
    internal class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Last-chance handler: wipe the screen before reporting any error")]
        static int Main()
        {
            if (Console.IsInputRedirected)
            {
                Console.Error.WriteLine("❌ Сід-фразу та пароль можна вводити лише в інтерактивному терміналі (ввід перенаправлено).");
                return 2;
            }

            Console.CancelKeyPress += (_, _) => ConsoleHelper.WipeIfSecretsTouched();

            try
            {
                UserInterface.Run();
            }
            catch (Exception ex)
            {
                ConsoleHelper.WipeIfSecretsTouched();
                Console.Error.WriteLine($"\n❌ Неочікувана помилка: {ex.Message}");
                Console.Error.WriteLine("Будь ласка, повідомте про цю помилку розробникам.");
                return 1;
            }

            ConsoleHelper.WipeIfSecretsTouched();
            Console.WriteLine("\nДякуємо за використання програми!");
            return 0;
        }
    }
}

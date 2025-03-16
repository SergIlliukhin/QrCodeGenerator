using System.Diagnostics.CodeAnalysis;

namespace QrCodeGenerator
{
    [SuppressMessage("Design", "CA1852:Seal internal types", Justification = "Program class is implicitly sealed")]
    internal class Program
    {
        static void Main()
        {
            UserInterface.Run();
        }
    }
}

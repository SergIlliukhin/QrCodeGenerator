using ZXing;
using ZXing.Common;
using System;
using System.Linq;
using SkiaSharp;

public class QRCodeReaderUtil
{
    public static string ReadQRCode(string filePath)
    {
        using var bitmap = SKBitmap.Decode(filePath);
        var width = bitmap.Width;
        var height = bitmap.Height;
        var pixels = bitmap.Pixels;
        
        var luminanceSource = new RGBLuminanceSource(
            pixels.Select(p => (byte)((p.Red + p.Green + p.Blue) / 3)).ToArray(),
            width, height);
            
        var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminanceSource));
        var reader = new MultiFormatReader();
        var result = reader.decode(binaryBitmap);
        return result?.Text ?? "Помилка зчитування QR-коду";
    }
}

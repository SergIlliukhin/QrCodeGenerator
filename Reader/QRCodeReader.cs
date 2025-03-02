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
        
        // Convert SKColor pixels to RGB byte array (3 bytes per pixel)
        var rgbBytes = new byte[width * height * 3];
        for (int i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            rgbBytes[i * 3] = color.Red;     // R
            rgbBytes[i * 3 + 1] = color.Green; // G
            rgbBytes[i * 3 + 2] = color.Blue;  // B
        }
            
        var luminanceSource = new RGBLuminanceSource(rgbBytes, width, height);
        var binaryBitmap = new BinaryBitmap(new HybridBinarizer(luminanceSource));
        var reader = new MultiFormatReader();
        var result = reader.decode(binaryBitmap);
        return result?.Text ?? "Помилка зчитування QR-коду";
    }
}

using ZXing;
using ZXing.QrCode;
using SkiaSharp;
using System.IO;

public class QRCodeGeneratorUtil
{
    public static void GenerateQRCode(string text, string filePath)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 400,
                Width = 400,
                Margin = 1
            }
        };

        var pixelData = writer.Write(text);
        
        using var surface = SKSurface.Create(new SKImageInfo(pixelData.Width, pixelData.Height));
        using var canvas = surface.Canvas;
        
        canvas.Clear(SKColors.White);
        
        using var skData = SKData.CreateCopy(pixelData.Pixels);
        using var image = SKImage.FromPixels(
            new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888),
            skData
        );
        canvas.DrawImage(image, 0, 0);
        
        using var data = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);
    }
}

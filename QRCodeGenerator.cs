using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Common;
using System;
using System.IO;
using SkiaSharp;

#nullable enable

namespace QrCodeGenerator
{
    public static class QRCodeGeneratorUtil
    {
        public static void GenerateQRCode(string content, string filePath)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Порожній зміст для QR-коду");
            }

            try
            {
                var writer = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        DisableECI = true,
                        CharacterSet = "UTF-8",
                        ErrorCorrection = ErrorCorrectionLevel.H,
                        Width = 800,
                        Height = 800,
                        Margin = 2
                    }
                };

                var pixelData = writer.Write(content);

                using var surface = SKSurface.Create(new SKImageInfo(pixelData.Width, pixelData.Height));
                using var canvas = surface.Canvas;
                
                // Clear the canvas
                canvas.Clear(SKColors.White);
                
                // Draw the QR code
                using var bitmap = new SKBitmap(new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Rgba8888));
                
                // Copy pixel data
                System.Runtime.InteropServices.Marshal.Copy(
                    pixelData.Pixels, 
                    0, 
                    bitmap.GetPixels(), 
                    pixelData.Pixels.Length);
                
                canvas.DrawBitmap(bitmap, 0, 0);
                
                // Save the image to a file
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"❌ Помилка генерації QR-коду: {ex.Message}", ex);
            }
        }
    }
}

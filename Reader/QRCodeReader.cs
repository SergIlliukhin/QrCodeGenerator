using ZXing;
using ZXing.Common;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using SkiaSharp;
using QrCodeGenerator.Shared;

namespace QrCodeGenerator.Reader
{
    internal static class QRCodeReaderUtil
    {
        private static readonly CompositeFormat QrNotFoundFormat = CompositeFormat.Parse(Resources.Messages.QrNotFound);
        internal static string ReadQRCode(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, QrNotFoundFormat, filePath));
        }

        try
        {
            using var bitmap = SKBitmap.Decode(filePath);
            if (bitmap == null)
            {
                throw new InvalidOperationException(Resources.Messages.InvalidImageFile);
            }

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
            
            if (result == null)
            {
                throw new InvalidOperationException(Resources.Messages.QrNotRecognized);
            }
            
            return result.Text;
        }
        catch (Exception ex) when (!(ex is FileNotFoundException))
        {
            throw new InvalidOperationException(Resources.Messages.QrReadError, ex);
        }
    }
}}

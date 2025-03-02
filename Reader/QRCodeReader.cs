using ZXing;
using System;
using System.Drawing;

public class QRCodeReaderUtil
{
    public static string ReadQRCode(string filePath)
    {
        BarcodeReader reader = new BarcodeReader();
        using (Bitmap bitmap = new Bitmap(filePath))
        {
            var result = reader.Decode(bitmap);
            return result?.Text ?? "Помилка зчитування QR-коду";
        }
    }
}

using QRCoder;

namespace Invoicing.Pdf;

/// <summary>Creates a PNG QR code from a payment descriptor.</summary>
public class QrCodeImageGenerator
{
    private const int PixelsPerModule = 8;

    public byte[] CreatePng(string text)
    {
        using var data = QRCodeGenerator.GenerateQrCode(text, QRCodeGenerator.ECCLevel.M);
        using var image = new PngByteQRCode(data);
        return image.GetGraphic(PixelsPerModule);
    }
}

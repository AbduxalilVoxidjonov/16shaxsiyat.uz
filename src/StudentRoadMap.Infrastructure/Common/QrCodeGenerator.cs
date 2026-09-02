using QRCoder;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `QRCoder` (`PngByteQRCode` — `System.Drawing`siz, Linux/Docker'da muammosiz) asosida QR kod
/// generatsiyasi (`prompts/14` MAXSUS DIQQAT #4).
/// </summary>
internal sealed class QrCodeGenerator : IQrCodeGenerator
{
    private const int PixelsPerModule = 10;

    public string GeneratePngBase64(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("QR kod mazmuni bo'sh bo'lishi mumkin emas.", nameof(content));
        }

        using var qrCodeGenerator = new QRCodeGenerator();
        using var qrCodeData = qrCodeGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        var bytes = pngQrCode.GetGraphic(PixelsPerModule);

        return Convert.ToBase64String(bytes);
    }
}

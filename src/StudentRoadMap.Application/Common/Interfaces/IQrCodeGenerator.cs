namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// QR kod generatsiyasi — maktab havolasini (`RegenerateLink`, `docs/07` 3.1-bo'lim) rasm
/// sifatida taqdim etish uchun (`prompts/14` MAXSUS DIQQAT #4: `QRCoder` paketi, PNG base64).
/// `Infrastructure`da amalga oshiriladi — grafik kutubxona `Application` qatlamiga tegishli emas.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>`content` (masalan `publicUrl`) uchun PNG QR kodini Base64 (data URI'siz, xom) shaklda qaytaradi.</summary>
    string GeneratePngBase64(string content);
}

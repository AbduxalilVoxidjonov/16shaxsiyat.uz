namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// RFC 6238 (TOTP) sekret generatsiyasi va kod tekshiruvi — `docs/08-auth-va-xavfsizlik.md`
/// 2-bo'lim: "30 s oyna ±1, sekret AES-256-GCM bilan shifrlangan". Sekret bu interfeys darajasida
/// OCHIQ (Base32) qaytariladi/qabul qilinadi — shifrlash/deshifrlash chaqiruvchida
/// (`IEncryptionService` orqali) sodir bo'ladi, `Infrastructure`da amalga oshiriladi.
/// </summary>
public interface ITotpService
{
    /// <summary>Yangi tasodifiy sekret — Base32 (RFC 4648, to'ldiruvchisiz, katta harf) kodlangan.</summary>
    string GenerateSecret();

    /// <summary>Autentifikator ilovalari (Google Authenticator va h.k.) uchun `otpauth://` provisioning havolasi.</summary>
    string BuildOtpauthUri(string secretBase32, string accountName, string issuer);

    /// <summary>
    /// `code`ni `secretBase32` asosida ±1 qadam (30 s) oynada tekshiradi. Qayta ishlatishga
    /// qarshi: `lastUsedStep`dan katta bo'lgan qadamlargina qabul qilinadi. Muvaffaqiyatda
    /// `matchedStep`ni qaytaradi — chaqiruvchi shu qiymatni `AdminUser.RegisterTotpStepUsed`ga beradi.
    /// </summary>
    bool TryValidate(string secretBase32, string code, DateTimeOffset now, long? lastUsedStep, out long matchedStep);
}

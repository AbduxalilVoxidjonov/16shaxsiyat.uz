namespace StudentRoadMap.Application.Identity.EnableTotp;

/// <summary>
/// `POST /api/auth/totp/enable` javobi — o'rnatishning BIRINCHI bosqichi (`docs/07` 2-bo'lim).
/// 2FA bu bosqichda HALI YOQILMAGAN; foydalanuvchi QR'ni skanerlab, `POST /api/auth/totp/confirm`
/// ga 6 xonali kod yuborishi kerak.
///
/// <para>
/// `QrCodePngBase64` — `IQrCodeGenerator.GeneratePngBase64(otpauthUri)` natijasi: maktab QR
/// kodi (`AdminSchoolDetailDto.QrCodeBase64`) bilan bir xil formatda — XOM base64 PNG, `data:`
/// prefiksisiz (prefiks frontendda qo'shiladi). `Secret` qo'lda kiritish uchun (kamera yo'q
/// yoki skaner ishlamagan holat) qoladi.
/// </para>
///
/// <para>
/// **Zaxira kodlar bu javobda YO'Q** — ular `confirm` javobida qaytadi (`ConfirmTotpResult`).
/// Sabab: tasdiqlanmagan o'rnatish uchun DB'ga 8 ta xeshlangan kod yozib qo'yish (a) hech
/// qachon yoqilmaydigan 2FA uchun keraksiz maxfiy material qoldiradi, (b) foydalanuvchini
/// "hisobga kirish kodlari" ni saqlashga majburlaydi, holbuki 2FA yoqilmagan — kodlar hech
/// narsani ochmaydi, (c) takroriy `enable` chaqiruvlarida eskirgan kodlar to'planib qolardi.
/// </para>
/// </summary>
/// <param name="Secret">Base32 sir — autentifikator ilovasiga qo'lda kiritish uchun.</param>
/// <param name="OtpauthUri">`otpauth://totp/...` provisioning havolasi.</param>
/// <param name="QrCodePngBase64">`OtpauthUri` ning QR kodi — xom base64 PNG.</param>
/// <param name="ExpiresAt">Kutish holatidagi sir shu vaqtdan keyin yaroqsiz (10 daqiqa).</param>
public sealed record EnableTotpResult(
    string Secret,
    string OtpauthUri,
    string QrCodePngBase64,
    DateTimeOffset ExpiresAt);

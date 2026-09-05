namespace StudentRoadMap.Application.Public.StartPublicSession;

/// <summary>
/// Ommaviy oqimdagi roziliknoma versiyasi (`Student.ConsentVersion`). Qiymat SERVER tomonidan
/// qo'yiladi — mijozdan qabul qilinmaydi: aks holda foydalanuvchi ixtiyoriy satr yuborib
/// "qaysi matnga rozilik berilgani" yozuvini soxtalashtira olardi. Roziliknoma matni
/// o'zgarganda bu qiymat oshiriladi va `Student.RecordConsent` orqali qayta so'raladi.
/// </summary>
internal static class PublicConsent
{
    public const string CurrentVersion = "1.0";

    /// <summary>Shu yoshdan kichik foydalanuvchi uchun ota-ona roziligi majburiy (`docs/08` 5-bo'lim).</summary>
    public const int ParentalConsentRequiredBelowAge = 18;
}

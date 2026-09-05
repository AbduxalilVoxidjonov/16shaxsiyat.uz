namespace StudentRoadMap.Application.PublicUsers.Common;

/// <summary>
/// Ommaviy (Telegram orqali kirgan) foydalanuvchi JWT'sining claim shartnomasi.
///
/// **Nima uchun alohida rol VA alohida `aud`:** superadmin oqimi bir belgi ham o'zgarmasligi
/// SHART (`SuperAdminPolicy` + 242 integratsiya testi). Faqat rolga tayanish yetarli emas —
/// bitta xato policy sozlamasi ikki auditoriyani aralashtirib yuborardi. Shu sabab ommaviy
/// token BOSHQA `aud` (`Jwt:PublicAudience`) bilan imzolanadi va API'da ALOHIDA
/// `JwtBearer` sxemasi (`PublicBearer`) orqali tekshiriladi: admin sxemasi ommaviy tokenni
/// `aud` mos kelmagani uchun hatto ROLGA yetib bormasdan rad etadi (va aksincha).
/// </summary>
public static class PublicUserClaims
{
    /// <summary>`role` claim qiymati — `AdminRole` qiymatlaridan (`SuperAdmin`) ATAYLAB farq qiladi.</summary>
    public const string Role = "PublicUser";

    /// <summary>`aud` uchun standart qiymat (`Jwt:PublicAudience` berilmasa).</summary>
    public const string DefaultAudience = "studentroadmap-public";
}

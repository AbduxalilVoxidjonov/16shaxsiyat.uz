using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// "Foydalanuvchiga natija ko'rsatiladimi" qoidasi — IKKI bayroqning **VA** (`&amp;&amp;`)
/// birlashmasi (P47 qarori, `School.ShowResultToStudent` izohi):
///
/// 1. `App:ShowResultToStudent` (GLOBAL) — **avariya rubilnigi (kill-switch)**. `false`
///    butun tizim bo'ylab (maktab ham, ommaviy makon ham) natijani yopadi — huquqiy talab
///    yoki insident holati uchun bitta env o'zgaruvchisi bilan. Standart qiymat `true`
///    (`IAppSettings.ShowResultToStudent` izohi: qaror endi MAKONGA topshirilgan).
/// 2. `School.ShowResultToStudent` (MAKON) — haqiqiy biznes qarori. Maktab uchun standart
///    `false` (natija psixolog orqali beriladi), ommaviy makon uchun `true`
///    (`School.CreatePublicSpace` — tashqi foydalanuvchi o'z natijasini ko'rmasa mahsulotning
///    ma'nosi qolmaydi).
///
/// Qoida BITTA joyda yozilgan — ikki chaqiruvchi (maktab oqimi va ommaviy kabinet) uni
/// har xil talqin qilishi mumkin emas.
/// </summary>
internal static class ShowResultPolicy
{
    public static bool IsAllowed(bool globalEnabled, School space) => IsAllowed(globalEnabled, space.ShowResultToStudent);

    /// <summary>
    /// Bir xil qoida, lekin makon bayrog'i allaqachon proyeksiya qilingan holat uchun
    /// (`ListMyAssessmentsQueryHandler` butun `School` agregatini tortmaydi).
    /// </summary>
    public static bool IsAllowed(bool globalEnabled, bool spaceEnabled) => globalEnabled && spaceEnabled;

    /// <summary>Rad javobidagi matn — ikkala bayroq uchun ham BIR XIL (qaysi bayroq yopganini oshkor qilmaydi).</summary>
    public const string ForbiddenMessageUz = "Natijani ko'rsatish hozircha o'chirilgan.";
}

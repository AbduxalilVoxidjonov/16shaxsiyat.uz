namespace StudentRoadMap.Domain.Schools;

/// <summary>
/// Makon turi (`docs/04` 2.1, `docs/05` 3-bo'lim). Platforma ikki oqimda ishlaydi:
///
/// • <see cref="School"/> — klassik maktab havolasi oqimi (slug + accessToken, ro'yxatdan
///   o'tish shart emas, o'quvchi maktab nomidan kiradi). Bu oqim O'ZGARMAYDI.
/// • <see cref="PublicSpace"/> — ommaviy (maktabsiz) foydalanuvchilar makoni. Telegram orqali
///   kirgan tashqi foydalanuvchining `Student`/`Assessment` yozuvlari AYNAN shu makonga
///   tegishli bo'ladi.
///
/// Nima uchun "virtual maktab" tanlandi: `Student.SchoolId` va `Assessment.SchoolId` domen va
/// EF darajasida MAJBURIY, `SchoolId` esa 42 ta ishchi kod faylida (asosan admin ro'yxat/eksport/
/// dashboard so'rovlarida) filtr sifatida ishlatiladi. Ustunni nullable qilish o'sha 42 faylni
/// qayta yozishni talab qilardi; makon turi esa BITTA ustun bilan ajratishni beradi va admin
/// panelda alohida bo'lim sifatida filtrlash imkonini qoldiradi.
///
/// Bazada AYNAN BITTA `PublicSpace` yozuvi bo'lishi ikki qatlamda kafolatlanadi:
/// domen (`School.CreatePublicSpace` + `Deactivate`/`MarkDeleted` qulflari) va DB
/// (`ux_schools_public_space` qisman unikal indeks — poyga holatiga qarshi yagona haqiqiy himoya).
/// </summary>
public enum SchoolKind
{
    School = 1,
    PublicSpace = 2,
}

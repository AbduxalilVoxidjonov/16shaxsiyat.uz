using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Schools;

/// <summary>
/// Admin "Maktablar" bo'limining KO'LAMI — faqat <see cref="SchoolKind.School"/>.
/// Ommaviy makon (<see cref="SchoolKind.PublicSpace"/>) bu bo'limda UMUMAN ko'rinmaydi:
/// u maktab emas, uning o'z bo'limi bor (`Admin/PublicSpace`).
///
/// <para>
/// <b>Nima uchun EF Core GLOBAL query filtri EMAS, balki aniq <c>Where</c>?</b>
/// Global filtr (<c>modelBuilder.Entity&lt;School&gt;().HasQueryFilter(s =&gt; s.Kind == SchoolKind.School)</c>)
/// butun <see cref="Microsoft.EntityFrameworkCore.DbContext"/> bo'ylab, YA'NI ADMIN'DAN
/// TASHQARIDA HAM ishlaydi va ommaviy makonni O'ZI uchun so'ragan joylardan ham yashirib
/// qo'yadi:
/// <list type="bullet">
///   <item><c>StartPublicSessionCommandHandler</c> — makonni AYNAN
///   <c>Where(s =&gt; s.Kind == SchoolKind.PublicSpace)</c> bilan qidiradi; global filtr uni
///   <c>null</c> qilib qaytarardi va butun ommaviy oqim jimgina
///   <c>409 PUBLIC_SPACE_NOT_CONFIGURED</c> ga tushardi.</item>
///   <item><c>DbSeeder.SeedPublicSpaceAsync</c> — mavjud makonni topa olmay, HAR SEEDDA
///   ikkinchisini yaratishga urinardi va <c>ux_schools_public_space</c> qisman unikal
///   indeksiga urilardi.</item>
///   <item>Yangi <c>Admin/PublicSpace</c> handlerlari — o'z bo'limining ma'lumotini ololmasdi.</item>
/// </list>
/// Ya'ni global filtr xatoni "ko'rinmas" qiladi: chaqiruvchi <c>IgnoreQueryFilters()</c> yozishni
/// unutsa, natija bo'sh qaytadi va HECH QANDAY xato chiqmaydi. Bundan tashqari <c>School</c> da
/// allaqachon <c>!IsDeleted</c> global filtri bor — <c>IgnoreQueryFilters()</c> EF Core'da
/// filtrlarni BITTALAB emas, BARCHASINI birdan o'chiradi, ya'ni ommaviy makonni ko'rish uchun
/// yozilgan <c>IgnoreQueryFilters()</c> yon ta'sir sifatida soft-delete qilingan maktablarni
/// ham qaytarib yuborardi (jimgina ma'lumot sizishi).
/// </para>
/// <para>
/// Shu sabab ko'lam ANIQ va MAHALLIY: faqat <c>Admin/Schools</c> handlerlari shu metodni
/// chaqiradi. Metod bitta joyda (bu fayl) — filtrni handlerma-handler qo'lda takrorlash
/// ajralib ketishga olib kelardi (<c>AdminStudentFilterBuilder</c> bilan bir xil ruh).
/// </para>
/// </summary>
internal static class AdminSchoolScope
{
    /// <summary>Faqat haqiqiy maktablar — ommaviy makon chiqarib tashlanadi.</summary>
    public static IQueryable<School> SchoolsOnly(this IQueryable<School> query) =>
        query.Where(s => s.Kind == SchoolKind.School);
}

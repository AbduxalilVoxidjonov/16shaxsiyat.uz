using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Common;

/// <summary>
/// Yozuvning MANBASI — o'quvchi/sessiya qaysi oqimdan kelgan:
/// <see cref="School"/> (maktab havolasi orqali) yoki <see cref="Public"/> (ommaviy makon,
/// Telegram orqali kirgan tashqi foydalanuvchi).
///
/// <para>
/// Enum EMAS, `const string` — chunki bu qiymatlar JSON javobiga TO'G'RIDAN-TO'G'RI chiqadi
/// (`docs/07` 4-bo'lim: enum'lar JSON'da string) va `?source=` query parametrida kiradi.
/// Bitta manba (`AdminSourceFilter`) filtr, DTO qiymati va parserni birga saqlaydi — ular
/// ajralib ketsa "ommaviy" deb filtrlangan ro'yxat "School" deb belgilangan qatorlarni
/// ko'rsatib qo'yardi.
/// </para>
/// </summary>
internal static class AdminSourceFilter
{
    /// <summary>Maktab havolasi oqimi (`SchoolKind.School`).</summary>
    public const string School = "School";

    /// <summary>Ommaviy makon oqimi (`SchoolKind.PublicSpace`).</summary>
    public const string Public = "Public";

    /// <summary>
    /// `?source=` parametrini o'qiydi. Noma'lum yoki bo'sh qiymat — `null`, ya'ni "filtr yo'q"
    /// (HAMMASI). Jimgina bo'sh ro'yxat qaytarilmaydi: noto'g'ri yozilgan `?source=maktab`
    /// filtrni O'CHIRADI, "hech narsa topilmadi" degan yolg'on javob bermaydi.
    /// </summary>
    public static string? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();

        if (string.Equals(trimmed, School, StringComparison.OrdinalIgnoreCase))
        {
            return School;
        }

        return string.Equals(trimmed, Public, StringComparison.OrdinalIgnoreCase) ? Public : null;
    }

    /// <summary>
    /// Ommaviy makonning `Id`si (bazada AYNAN BITTA — `ux_schools_public_space` qisman unikal
    /// indeks). Topilmasa `null` (seed bajarilmagan baza).
    ///
    /// <para>
    /// **Nima uchun `Id`, `Kind` bo'yicha `EXISTS` sub-so'rov emas:** `Student`/`Assessment` da
    /// `Kind` ustuni yo'q, filtr `schools` ga bog'lanishni talab qilardi. Makon BITTA bo'lgani
    /// uchun uning `Id`sini BIR MARTA o'qib, keyingi filtr oddiy `school_id = @id` (yoki `<>`)
    /// bo'lib qoladi — `ix_students_school_*` / `ix_assessments_school_status` indekslariga
    /// AYNAN mos tushadi va JOIN/EXISTS umuman kerak bo'lmaydi.
    /// </para>
    /// <para>
    /// `Application` qatlami `DbSeeder.PublicSpaceSchoolId` konstantasiga bog'lana olmaydi
    /// (`docs/06` 3-bo'lim) — shu sabab qidiruv TUR bo'yicha
    /// (`StartPublicSessionCommandHandler` dagi bilan bir xil sabab).
    /// </para>
    /// </summary>
    public static async Task<Guid?> FindPublicSpaceIdAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var ids = await executor.ToListAsync(
            context.AsNoTracking(context.Schools)
                .Where(s => s.Kind == SchoolKind.PublicSpace)
                .Select(s => s.Id),
            cancellationToken).ConfigureAwait(false);

        return ids.Count == 0 ? null : ids[0];
    }

    /// <summary>
    /// Qator qaysi manbadan — `schoolId` ommaviy makonniki bo'lsa <see cref="Public"/>,
    /// aks holda <see cref="School"/>. Makon bazada bo'lmasa hamma narsa maktab hisoblanadi.
    /// </summary>
    public static string SourceOf(Guid schoolId, Guid? publicSpaceId) =>
        publicSpaceId.HasValue && schoolId == publicSpaceId.Value ? Public : School;
}

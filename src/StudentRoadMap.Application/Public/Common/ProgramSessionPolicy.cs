using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// BR-1 (`docs/02` 4-bo'lim) va BR-5 uchun sessiya qidirish — **`(o'quvchi, dastur)` juftligi**
/// bo'yicha. Ikkala sessiya ochish oqimi (`StartSessionCommandHandler` — maktab,
/// `StartPublicSessionCommandHandler` — ommaviy makon) AYNAN shu ikki so'rovdan foydalanadi;
/// mantiq ilgari har ikkisida nusxa bo'lib turardi va bittasida o'zgarib ikkinchisida qolib
/// ketishi mumkin edi (`AssessmentTestAttacher` bilan bir xil sabab, P47).
///
/// **Egasining qarori (2026-09-07):** cheklov o'quvchi bo'yicha UMUMIY emas, dastur bo'yicha.
/// Har dastur — boshqa metodika to'plami; bir odamning ikkinchi dasturni topshirishi "takror"
/// emas. Shu sabab:
/// - A dasturni yakunlagan o'quvchi 90 kun ichida A ni qayta boshlay olmaydi (`409`), lekin
///   B ni boshlay oladi (`201`);
/// - A da yarim qolgan sessiya bor bo'lsa, A so'ralsa o'sha qaytariladi (`resumed`), B so'ralsa
///   YANGI sessiya ochiladi — natijada bir o'quvchida bir vaqtda bir nechta (har dasturda
///   ko'pi bilan bitta) tugallanmagan sessiya bo'lishi mumkin. Buni iste'molchilar
///   (`GET /api/me/assessments`, admin "oxirgi sessiya") hisobga oladi.
///
/// Dastur tanlash (`ResolveProgramAsync`) bu tekshiruvlardan OLDIN bajarilishi shart —
/// aks holda qaysi dasturga qarab tekshirish noma'lum.
/// </summary>
internal static class ProgramSessionPolicy
{
    /// <summary>BR-1 qayta topshirish oynasi — bir dastur uchun.</summary>
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromDays(90);

    /// <summary>
    /// Shu o'quvchining shu dasturdagi eng so'nggi tugallanmagan (`Draft`/`InProgress`)
    /// sessiyasi yoki `null`. Muddati o'tganini ham qaytaradi — BR-5 qarori (davom ettirish
    /// yoki `Abandoned`) chaqiruvchida.
    /// </summary>
    /// <remarks>
    /// TODO(P30): server tomonida `OrderByDescending(DateTimeOffset)` ataylab ishlatilmayapti
    /// — SQLite provayderi (integratsiya sinovlari muhiti, Docker/PostgreSQL yo'q)
    /// `DateTimeOffset` bo'yicha ORDER BY'ni tarjima qila olmaydi (`NotSupportedException`).
    /// Texnik qarz: sinovlar Postgres'dagi haqiqiy (server-side ORDER BY) yo'lni sinamaydi.
    /// Bir `(o'quvchi, dastur)` juftligida amalda 0–1 ta tugallanmagan sessiya bo'lgani uchun
    /// mijoz tomonida saralash xavfsiz (kichik to'plam, to'g'ri natija). P30 (E2E)da
    /// Testcontainers bilan haqiqiy Postgres ustida qayta ko'rilib, kerak bo'lsa server-side
    /// `OrderByDescending`ga qaytariladi.
    /// </remarks>
    public static async Task<Assessment?> FindUnfinishedAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid studentId,
        Guid programId,
        CancellationToken cancellationToken)
    {
        var candidates = await executor.ToListAsync(
            context.Assessments.Where(a =>
                a.StudentId == studentId &&
                a.ProgramId == programId &&
                (a.Status == AssessmentStatus.Draft || a.Status == AssessmentStatus.InProgress)),
            cancellationToken).ConfigureAwait(false);

        return candidates.OrderByDescending(a => a.StartedAt).FirstOrDefault();
    }

    /// <summary>
    /// BR-1: shu o'quvchi shu dasturni so'nggi <see cref="DuplicateWindow"/> ichida yakunlaganmi.
    /// </summary>
    /// <remarks>
    /// TODO(P30): `CompletedAt >= ...` server tomonida SQLite'da tarjima qilinmaydi
    /// (yuqoridagi izohdagi sabab), shu sabab oyna mijoz tomonida filtrlanadi — `(o'quvchi,
    /// dastur)` bo'yicha yakunlangan sessiyalar to'plami kichik.
    /// </remarks>
    public static async Task<bool> HasCompletedWithinWindowAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid studentId,
        Guid programId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var completed = await executor.ToListAsync(
            context.Assessments.Where(a =>
                a.StudentId == studentId &&
                a.ProgramId == programId &&
                a.CompletedAt != null),
            cancellationToken).ConfigureAwait(false);

        var windowStart = now - DuplicateWindow;

        return completed.Any(a => a.CompletedAt!.Value >= windowStart);
    }
}

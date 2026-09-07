using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Application.Public.GetSession;

/// <summary>
/// Sessiya "qayerda to'xtagan" hisobining YAGONA manbai — `GetSessionStateQueryHandler`
/// (ommaviy `GET /api/public/sessions/me`) va admin ommaviy makon ro'yxati
/// (`Admin/PublicSpace/ListUsers`) ikkalasi shu yerdan o'qiydi. Ilgari mantiq faqat
/// handler ichida edi; admin ro'yxati uni TAKRORLAMASLIGI uchun (2026-09-07) shu sinfga
/// chiqarildi — xatti-harakat o'zgarmagan.
///
/// <para>
/// Qoida (`docs/07` 1.3 namunasi): testlar `DisplayOrder` bo'yicha tartiblanadi; birinchi
/// `Completed` BO'LMAGAN test — JORIY test; undan keyingi barcha testlar `"Locked"`
/// (proyeksiya darajasidagi holat, domen `TestStatus` enum'ida yo'q). Hamma test yakunlangan
/// bo'lsa joriy test yo'q (`null`).
/// </para>
/// </summary>
internal static class SessionProgressCalculator
{
    /// <summary>Proyeksiya holati — domen enum'ida yo'q, faqat javobda ko'rinadi.</summary>
    public const string LockedStatus = "Locked";

    /// <summary>
    /// `DisplayOrder` bo'yicha TARTIBLANGAN ro'yxatda joriy testning indeksi (0 dan);
    /// barchasi `Completed` bo'lsa `null`.
    /// </summary>
    public static int? FindCurrentIndex(IReadOnlyList<AssessmentTest> orderedTests)
    {
        for (var i = 0; i < orderedTests.Count; i++)
        {
            if (orderedTests[i].Status != TestStatus.Completed)
            {
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// `index`-testning ommaviy holat satri: joriy testdan keyingilar `"Locked"`, qolganlar
    /// o'z `TestStatus` nomi (`NotStarted`/`InProgress`/`Completed`).
    /// </summary>
    public static string ProjectStatus(IReadOnlyList<AssessmentTest> orderedTests, int index, int? currentIndex)
    {
        if (currentIndex.HasValue && index > currentIndex.Value)
        {
            return LockedStatus;
        }

        return orderedTests[index].Status.ToString();
    }

    /// <summary>Yakunlangan test bloklari soni (tartibga bog'liq emas — haqiqiy `Completed` sanaladi).</summary>
    public static int CountCompleted(IReadOnlyList<AssessmentTest> tests) =>
        tests.Count(t => t.Status == TestStatus.Completed);

    /// <summary>
    /// Butun sessiya bo'yicha foiz — `round(100 · answered / total)`, savol yo'q bo'lsa `0`
    /// (`GetSessionStateResult.ProgressPercent` shartnomasi o'zgarmagan).
    /// </summary>
    public static int ProgressPercent(IReadOnlyList<AssessmentTest> tests)
    {
        var totalAnswered = tests.Sum(t => t.AnsweredCount);
        var totalQuestions = tests.Sum(t => t.TotalCount);
        return totalQuestions == 0
            ? 0
            : (int)Math.Round(100.0 * totalAnswered / totalQuestions, MidpointRounding.AwayFromZero);
    }
}

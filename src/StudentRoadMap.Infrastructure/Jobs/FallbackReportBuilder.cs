using System.Text.Json;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Infrastructure.Jobs;

/// <summary>
/// Barcha AI provayder/urinishlar muvaffaqiyatsiz bo'lganda ko'rsatiladigan shablon hisobot —
/// `TypeCatalog` (tip tavsifi, kuchli tomonlar) + `CareerMap` (Holland kodi bo'yicha
/// yo'nalishlar) asosida (`docs/09-ai-analiz-moduli.md` 11-bo'lim, P18). Bu — haqiqiy AI
/// chaqiruvi EMAS, faqat allaqachon bazada bor katalog ma'lumotlarini yig'adi.
/// </summary>
internal static class FallbackReportBuilder
{
    private const int MaxCareerSuggestions = 5;

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = null };

    public sealed record FallbackContent(
        string Summary,
        string PersonalityPortrait,
        string StrengthsJson,
        string GrowthAreasJson,
        string CareerSuggestionsJson,
        string TeacherNotes,
        string ParentNotes,
        string AttentionFlagsJson);

    public static async Task<FallbackContent> BuildAsync(
        IReadOnlyList<TestResult> testResults,
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var mbti = testResults.FirstOrDefault(r => r.TestCode == "MBTI16");
        var riasec = testResults.FirstOrDefault(r => r.TestCode == "RIASEC");

        TypeCatalogEntry? typeEntry = null;
        if (!string.IsNullOrEmpty(mbti?.ResultCode))
        {
            typeEntry = await executor.FirstOrDefaultAsync(
                context.AsNoTracking(context.TypeCatalog).Where(t => t.Code == mbti.ResultCode),
                cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<CareerMapEntry> careerEntries = [];
        if (!string.IsNullOrEmpty(riasec?.ResultCode))
        {
            var codeLetters = riasec.ResultCode.ToCharArray();
            var allEntries = await executor.ToListAsync(
                context.AsNoTracking(context.CareerMap).OrderBy(c => c.RelevanceOrder),
                cancellationToken).ConfigureAwait(false);

            careerEntries = allEntries
                .Where(c => c.HollandCode.All(letter => codeLetters.Contains(letter)))
                .OrderBy(c => c.RelevanceOrder)
                .DistinctBy(c => c.FieldNameUz)
                .Take(MaxCareerSuggestions)
                .ToList();
        }

        var summary = typeEntry is not null
            ? $"Avtomatik shablon hisobot: AI tahlili hozircha muvaffaqiyatli yakunlanmadi, lekin test natijalaringiz saqlangan. " +
              $"Umumiy metodika ma'lumotlariga ko'ra sizning tipingiz — \"{typeEntry.NameUz}\". {typeEntry.ShortDescriptionUz}"
            : "Avtomatik shablon hisobot: AI tahlili hozircha muvaffaqiyatli yakunlanmadi, lekin test natijalaringiz saqlangan. " +
              "To'liq shaxsiylashtirilgan tahlil tayyor bo'lgach bu yerda ko'rsatiladi.";

        var personalityPortrait = typeEntry is not null
            ? (string.IsNullOrWhiteSpace(typeEntry.LongDescriptionUz) ? typeEntry.ShortDescriptionUz : typeEntry.LongDescriptionUz)
            : "Shaxsiyat portreti hozircha AI orqali tuzilmadi. Test ballaringiz o'quvchi profilida to'liq ko'rinadi.";

        var strengths = typeEntry?.Strengths ?? [];
        var growthAreas = typeEntry?.GrowthAreas ?? [];

        var careerSuggestions = careerEntries.Count > 0
            ? careerEntries.Select(c => new
            {
                field = c.FieldNameUz,
                why = string.IsNullOrWhiteSpace(c.DescriptionUz) ? "Qiziqishlar natijasiga (Holland kodi) mos yo'nalish." : c.DescriptionUz,
                nextSteps = c.ExampleProfessions,
            })
            : (typeEntry?.CareerHints ?? []).Select(hint => new
            {
                field = hint,
                why = "Shaxsiyat tipiga mos umumiy yo'nalish.",
                nextSteps = (IReadOnlyList<string>)[],
            });

        const string TeacherNotesText = "Bu — avtomatik shablon hisobot (AI tahlili emas). To'liq shaxsiylashtirilgan " +
            "tavsiyalar uchun superadmin panelida \"Qayta urinish\" tugmasidan foydalaning.";
        const string ParentNotesText = "Farzandingizning to'liq shaxsiylashtirilgan AI tahlili hozircha tayyor emas — " +
            "test natijalari saqlangan, tez orada to'liq hisobot ko'rinadi.";

        var attentionFlags = new List<string> { "Bu avtomatik shablon hisobot — AI tahlili hali muvaffaqiyatli yakunlanmagan." };

        return new FallbackContent(
            summary,
            personalityPortrait,
            JsonSerializer.Serialize(strengths, SerializerOptions),
            JsonSerializer.Serialize(growthAreas, SerializerOptions),
            JsonSerializer.Serialize(careerSuggestions, SerializerOptions),
            TeacherNotesText,
            ParentNotesText,
            JsonSerializer.Serialize(attentionFlags, SerializerOptions));
    }
}

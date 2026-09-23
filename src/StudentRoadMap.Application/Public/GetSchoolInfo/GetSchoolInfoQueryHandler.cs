using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Admin.Settings.RegistrationForm;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Public.GetSchoolInfo;

/// <summary>
/// `docs/07` 1.1-bo'lim: `slug` bo'yicha maktab topiladi, `accessToken` **fixed-time compare**
/// bilan solishtiriladi (`docs/08-auth-va-xavfsizlik.md` 3-bo'lim). Noto'g'ri token bo'lsa
/// maktabning mavjudligini oshkor qilmaslik uchun xuddi shu `NOT_FOUND` qaytariladi
/// (topilmagan slug bilan bir xil javob).
///
/// **Havola ochilishi hisoblagichi (`prompts/15` vazifa 1, 2026-09-02):** bu — dashboard
/// voronkasining ENG YUQORI bo'g'ini (`school_link_views`). Handler QUERY bo'lsa ham (odatda
/// `docs/06` 4-bo'lim: "Query'lar READ-ONLY") atayin bitta yon ta'sirga ega — `IncrementSchoolLinkViewAsync`
/// atomik xom SQL orqali (na `_context.Add`, na `SaveChangesAsync` — `TransactionBehavior`
/// FAQAT `*Command` so'rovlarini o'raydi, bu yerga kerak emas, chunki SQL statement o'zi atomik).
/// Hisoblagich **FAQAT** token to'g'ri VA maktab faol bo'lganda oshiriladi (404/410 holatida
/// OSHIRILMAYDI) — pastdagi ikkita erta `return`dan KEYIN chaqiriladi. Bu — QASDDAN qilingan
/// istisno (`docs/06` 4-bo'lim, PM qarori 2026-09-02): telemetriya hisoblagichi, alohida
/// endpoint qilish yomonroq bo'lardi (qo'shimcha so'rov, mijoz o'tkazib yuborishi mumkin,
/// poyga holati).
///
/// **`409 NO_PROGRAM_AVAILABLE` holatida hisoblagich OSHIRILADI** (2026-09-03 qarori). Bu —
/// ataylab: o'quvchi havolani CHINDAN ochgan, faqat test tayyor emas. Agar bu holatda
/// hisoblagich oshmasa, dastursiz davrdagi barcha ochilishlar YO'QOLARDI va dashboard
/// voronkasining eng yuqori bo'g'ini (`school_link_views`) jimgina noto'g'ri bo'lardi —
/// aynan admin "nega hech kim kirmayapti?" deb so'ragan paytda. Shu sabab chaqiruv dastur
/// tekshiruvidan OLDIN turadi (pastga qarang). `404`/`410` da esa oshirilmaydi: u yerda
/// havolaning O'ZI noto'g'ri yoki maktab ataylab o'chirilgan.
///
/// **FAIL-OPEN shart (PM qarori, 2026-09-02):** hisoblagich yozuvi MUVAFFAQIYATSIZ bo'lsa
/// (DB band, deadlock, cheklov buzilishi) landing sahifasi (`GET .../schools/{slug}`) YIQILMASLIGI
/// kerak — telemetriya nosozligi mahsulotdan MUHIMROQ bo'lib qolmasligi kerak. Shu sabab
/// chaqiruv `try/catch` bilan o'raladi: xato `LogWarning` bilan yoziladi va YUTILADI, javob
/// baribir muvaffaqiyatli qaytadi.
/// </summary>
internal sealed class GetSchoolInfoQueryHandler : IRequestHandler<GetSchoolInfoQuery, Result<GetSchoolInfoResult>>
{
    // Rozilik matni — loyiha egasi qarori (2026-09-23, PROGRESS.md ochiq savol #2 yopildi):
    // bir qatorlik qisqa matn.
    private const string ConsentTextPlaceholder =
        "Ma'lumotlarim ta'lim maqsadida ishlatilishiga roziman.";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ILogger<GetSchoolInfoQueryHandler> _logger;

    public GetSchoolInfoQueryHandler(
        IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, ILogger<GetSchoolInfoQueryHandler> logger)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<GetSchoolInfoResult>> Handle(GetSchoolInfoQuery request, CancellationToken cancellationToken)
    {
        var school = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Schools).Where(s => s.Slug == SchoolSlug.FromExisting(request.Slug)),
            cancellationToken).ConfigureAwait(false);

        if (school is null || !TokensMatch(school.AccessToken, request.AccessToken))
        {
            // Token noto'g'ri bo'lsa ham maktab topilmagan bilan bir xil javob — mavjudligini oshkor qilmaslik uchun.
            return Result.Failure<GetSchoolInfoResult>(new Error(ProblemCodes.NotFound, "Havola topilmadi."));
        }

        if (!school.IsActive)
        {
            return Result.Failure<GetSchoolInfoResult>(new Error(ProblemCodes.SchoolInactive, "Ushbu maktab havolasi hozircha faol emas."));
        }

        // Faqat shu nuqtadan — havola va token TO'G'RI VA maktab FAOL tasdiqlangach — atomik
        // oshiriladi (sinf izohiga qarang). 404/410 holatlarida yuqoridagi erta `return`lar
        // bilan chiqib ketiladi, hisoblagich OSHIRILMAYDI.
        //
        // FAIL-OPEN (PM qarori, 2026-09-02): bu — telemetriya, mahsulot ZANJIRINING o'zi EMAS.
        // Hisoblagich yozuvi (DB band, deadlock, cheklov buzilishi va h.k.) muvaffaqiyatsiz
        // bo'lsa ham, o'quvchi landing sahifasiga (bu so'rovning HAQIQIY maqsadi) kira olishi
        // SHART — shu sabab istisno bu yerda YUTILADI (faqat `LogWarning`), pastga OTILMAYDI.
        var dateUtc = DateOnly.FromDateTime(_dateTime.UtcNow.UtcDateTime);
        try
        {
            await _context.IncrementSchoolLinkViewAsync(school.Id, dateUtc, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Havola ochilishi hisoblagichini oshirib bo'lmadi (schoolId={SchoolId}) — landing sahifasi baribir qaytariladi.", school.Id);
        }

        var (programs, tests) = await BuildProgramsAsync(school.Id, cancellationToken).ConfigureAwait(false);

        // `docs/07` 1.1 (2026-09-03, jonli hodisadan keyin): havola VA token to'g'ri, maktab
        // faol — lekin bironta mavjud dastur yo'q. Ilgari bu holat `200` + bo'sh `programs[]`
        // qaytarardi; mijoz uchun "havola noto'g'ri" (o'quvchi maktabga havolani qayta so'raydi)
        // va "test hali tayyorlanmagan" (maktab admini dasturni yoqishi kerak) BUTUNLAY boshqa
        // harakat talab qiladi, shu sabab alohida kod ajratildi.
        //
        // XAVFSIZLIK: bu javob slug MAVJUDLIGINI tasdiqlaydi (noma'lum slug `NOT_FOUND` oladi).
        // Maktab havolasi o'quvchilarga ochiq tarqatiladi — slug maxfiy emas, shu sabab qabul
        // qilinadi. Javobda maktab haqida HECH QANDAY qo'shimcha ma'lumot yo'q (nom, viloyat,
        // testlar) — faqat holat va umumiy xabar.
        if (programs.Count == 0)
        {
            // DIQQAT: havola ochilishi hisoblagichi YUQORIDA allaqachon oshirilgan — bu ATAYIN
            // (sinf izohiga qarang). O'quvchi havolani chindan ochgan; voronkaning yuqori
            // bo'g'ini yo'qolmasligi kerak.

            return Result.Failure<GetSchoolInfoResult>(new Error(
                ProblemCodes.NoProgramAvailable,
                "Hozircha test mavjud emas — maktabingizga murojaat qiling."));
        }

        var result = new GetSchoolInfoResult(
            school.Id,
            school.Name,
            school.Region,
            school.District,
            RequiresAccessCode: !string.IsNullOrEmpty(school.AccessCode),
            tests,
            TotalEstimatedMinutes: tests.Sum(t => t.EstimatedMinutes),
            ConsentText: ConsentTextPlaceholder,
            Programs: programs);

        return Result.Success(result);
    }

    /// <summary>
    /// `prompts/34` C8-band — maktab uchun mavjud dasturlar (`ProgramAvailability`), har biri
    /// uchun test/savol soni va taxminiy vaqt. Har dastur uchun test ro'yxati `ProgramTestCatalog`
    /// dan olinadi — bu AYNAN sessiya (`AssessmentTestAttacher`) biriktiradigan ro'yxat bilan bir
    /// xil manba (P52, jonli hodisadan keyin: ikkalasi ajralib ketmasligi shart).
    ///
    /// Yuqori darajadagi (dasturga bog'liq bo'lmagan) `Tests` — shu yerda MAVJUD dasturlar
    /// bo'yicha BIRLASHMA sifatida ham yig'iladi (`testsByDefinitionId`, `TestDefinitionId`
    /// bo'yicha takrorlanmaydi — bitta test bir nechta dasturda bo'lishi mumkin).
    /// </summary>
    private async Task<(IReadOnlyList<PublicProgramSummaryDto> Programs, IReadOnlyList<PublicTestCatalogItemDto> Tests)> BuildProgramsAsync(
        Guid schoolId, CancellationToken cancellationToken)
    {
        var availablePrograms = await ProgramAvailability.GetAvailableProgramsAsync(_context, _executor, schoolId, cancellationToken).ConfigureAwait(false);

        // P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2): GLOBAL sozlama bir marta o'qiladi,
        // har dastur uchun faqat ustunlik (batareya bo'lsa `birthDate`/`grade` → `Required`)
        // qo'llanadi — `AssessmentProgram.RegistrationFields` (§9.5, eskirgan) ENDI O'QILMAYDI.
        var globalDefinition = await RegistrationFormResolver.GetGlobalDefinitionAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        var programs = new List<PublicProgramSummaryDto>(availablePrograms.Count);
        var testsByDefinitionId = new Dictionary<Guid, PublicTestCatalogItemDto>();

        foreach (var program in availablePrograms)
        {
            var items = await ProgramTestCatalog.GetTestsAsync(_context, _executor, program.Id, cancellationToken).ConfigureAwait(false);

            var programTests = items
                .Select(i => new PublicTestCatalogItemDto(i.Code, i.NameUz, NormalizeDescription(i.DescriptionUz), i.ActiveQuestionCount, i.EstimatedMinutes, i.Order))
                .ToList();

            // `docs/06` 8-bo'lim: dasturda ilmiy batareya BO'LMASLIGI mumkin — mezon
            // `PersonalityBattery` domen qoidasida, bu yerda kod ro'yxati YO'Q.
            var hasPersonalityBattery = items.Any(i => PersonalityBattery.Includes(i.Kind, i.ScoringMode));
            var effectiveDefinition = RegistrationFormResolver.ApplyProgramOverride(globalDefinition, hasPersonalityBattery);

            programs.Add(new PublicProgramSummaryDto(
                program.Code,
                program.NameUz,
                program.DescriptionUz,
                TestCount: items.Count,
                QuestionCount: items.Sum(i => i.ActiveQuestionCount),
                EstimatedMinutes: items.Sum(i => i.EstimatedMinutes),
                HasPersonalityBattery: hasPersonalityBattery,
                Tests: programTests,
                RegistrationMode: program.RegistrationMode.ToString(),
                RegistrationFields: RegistrationFieldsMapping.ToDto(RegistrationFieldsMapping.FromCoreFields(effectiveDefinition.CoreFields)),
                RegistrationForm: RegistrationFormSettingsMapping.ToDto(effectiveDefinition)));

            foreach (var item in items)
            {
                // Bitta test bir nechta mavjud dasturda bo'lishi mumkin — birinchi uchragan
                // dastur tartibida qoldiriladi, takrorlanmaydi (`TestDefinitionId` kaliti).
                testsByDefinitionId.TryAdd(item.TestDefinitionId, new PublicTestCatalogItemDto(item.Code, item.NameUz, NormalizeDescription(item.DescriptionUz), item.ActiveQuestionCount, item.EstimatedMinutes, item.Order));
            }
        }

        var tests = testsByDefinitionId.Values.OrderBy(t => t.Order).ToList();

        return (programs, tests);
    }

    /// <summary>Bo'sh yoki faqat bo'shliqdan iborat tavsif <c>null</c> sifatida qaytadi.</summary>
    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Fixed-time solishtirish (`docs/08` 3-bo'lim). Uzunlik farqi ma'lumot sizdirmaydi —
    /// token uzunligi (43 belgi, Base64Url 32 bayt) baribir ochiq/taxmin qilinadigan format.
    /// </summary>
    private static bool TokensMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided ?? string.Empty);

        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

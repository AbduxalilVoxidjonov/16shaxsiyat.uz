using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
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
    // TODO: rasmiy rozilik matni kutilmoqda (PROGRESS.md ochiq savol #2, loyiha egasidan javob
    // kelgach almashtiriladi). Hozircha vaqtinchalik, umumiy shakldagi matn ishlatilmoqda.
    private const string ConsentTextPlaceholder =
        "Farzandimning \"Shaxsiyat\" platformasida psixologik-pedagogik testlardan o'tishiga " +
        "va natijalarning ta'lim maqsadlarida (o'quvchi profili, maktab hisobotlari) qayta " +
        "ishlanishiga roziman. Ma'lumotlar faqat maktab va superadmin tomonidan ko'riladi.";

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

        var testDefinitions = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestDefinitions)
                .Where(t => t.Status == TestDefinitionStatus.Published && t.IsActive)
                .OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var tests = new List<PublicTestCatalogItemDto>(testDefinitions.Count);
        foreach (var testDefinition in testDefinitions)
        {
            var questionCount = await _executor.CountAsync(
                _context.AsNoTracking(_context.Questions).Where(q => q.TestDefinitionId == testDefinition.Id && q.IsActive),
                cancellationToken).ConfigureAwait(false);

            tests.Add(new PublicTestCatalogItemDto(
                testDefinition.Code,
                testDefinition.NameUz,
                questionCount,
                testDefinition.EstimatedMinutes,
                testDefinition.DisplayOrder));
        }

        var programs = await BuildProgramsAsync(school.Id, cancellationToken).ConfigureAwait(false);

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

    /// <summary>`prompts/34` C8-band — maktab uchun mavjud dasturlar (`ProgramAvailability`), har biri uchun test/savol soni va taxminiy vaqt.</summary>
    private async Task<IReadOnlyList<PublicProgramSummaryDto>> BuildProgramsAsync(Guid schoolId, CancellationToken cancellationToken)
    {
        var availablePrograms = await ProgramAvailability.GetAvailableProgramsAsync(_context, _executor, schoolId, cancellationToken).ConfigureAwait(false);

        var programs = new List<PublicProgramSummaryDto>(availablePrograms.Count);
        foreach (var program in availablePrograms)
        {
            var programTestDefinitionIds = await _executor.ToListAsync(
                _context.AsNoTracking(_context.ProgramTests).Where(pt => pt.ProgramId == program.Id).Select(pt => pt.TestDefinitionId),
                cancellationToken).ConfigureAwait(false);

            var programTestDefinitions = await _executor.ToListAsync(
                _context.AsNoTracking(_context.TestDefinitions)
                    .Where(t => programTestDefinitionIds.Contains(t.Id) && t.Status == TestDefinitionStatus.Published && t.IsActive),
                cancellationToken).ConfigureAwait(false);

            var questionCount = 0;
            foreach (var testDefinition in programTestDefinitions)
            {
                questionCount += await _executor.CountAsync(
                    _context.AsNoTracking(_context.Questions).Where(q => q.TestDefinitionId == testDefinition.Id && q.IsActive),
                    cancellationToken).ConfigureAwait(false);
            }

            programs.Add(new PublicProgramSummaryDto(
                program.Code,
                program.NameUz,
                program.DescriptionUz,
                TestCount: programTestDefinitions.Count,
                QuestionCount: questionCount,
                EstimatedMinutes: programTestDefinitions.Sum(t => t.EstimatedMinutes),
                // `docs/06` 8-bo'lim: dasturda ilmiy batareya BO'LMASLIGI mumkin — mezon
                // `PersonalityBattery` domen qoidasida, bu yerda kod ro'yxati YO'Q.
                HasPersonalityBattery: PersonalityBattery.ContainedIn(programTestDefinitions)));
        }

        return programs;
    }

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

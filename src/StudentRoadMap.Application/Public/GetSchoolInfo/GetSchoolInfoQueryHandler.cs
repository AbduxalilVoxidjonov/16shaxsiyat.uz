using System.Security.Cryptography;
using System.Text;
using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Public.GetSchoolInfo;

/// <summary>
/// `docs/07` 1.1-bo'lim: `slug` bo'yicha maktab topiladi, `accessToken` **fixed-time compare**
/// bilan solishtiriladi (`docs/08-auth-va-xavfsizlik.md` 3-bo'lim). Noto'g'ri token bo'lsa
/// maktabning mavjudligini oshkor qilmaslik uchun xuddi shu `NOT_FOUND` qaytariladi
/// (topilmagan slug bilan bir xil javob).
/// </summary>
internal sealed class GetSchoolInfoQueryHandler : IRequestHandler<GetSchoolInfoQuery, Result<GetSchoolInfoResult>>
{
    // TODO: rasmiy rozilik matni kutilmoqda (PROGRESS.md ochiq savol #2, loyiha egasidan javob
    // kelgach almashtiriladi). Hozircha vaqtinchalik, umumiy shakldagi matn ishlatilmoqda.
    private const string ConsentTextPlaceholder =
        "Farzandimning \"Salohiyat\" platformasida psixologik-pedagogik testlardan o'tishiga " +
        "va natijalarning ta'lim maqsadlarida (o'quvchi profili, maktab hisobotlari) qayta " +
        "ishlanishiga roziman. Ma'lumotlar faqat maktab va superadmin tomonidan ko'riladi.";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetSchoolInfoQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
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

        var result = new GetSchoolInfoResult(
            school.Id,
            school.Name,
            school.Region,
            school.District,
            RequiresAccessCode: !string.IsNullOrEmpty(school.AccessCode),
            tests,
            TotalEstimatedMinutes: tests.Sum(t => t.EstimatedMinutes),
            ConsentText: ConsentTextPlaceholder);

        return Result.Success(result);
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

using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetStudentResult;

/// <summary>
/// `docs/07` 1.9-bo'lim. Read-only — `AsNoTracking`.
///
/// Javob uch xil:
/// - `403 FORBIDDEN` — superadmin sozlamasi (`App:ShowResultToStudent`, default `false`,
///   `prompts/12` cheklovi 8) o'chirilgan bo'lsa. Boshqa hech narsani tekshirmasdan, ENG
///   BIRINCHI qaytariladi — DB'ga bekorga murojaat qilinmaydi.
/// - `202` (`Result.Success(null)`, kontroller `Accepted()`ga aylantiradi) — sessiya hali
///   `Analyzed` holatiga yetmagan (P18'gacha, AI ulanmagan bo'lsa, bu HAR DOIM shu holat).
/// - `200` — `Analyzed` holatida, `MBTI16`/`RIASEC` `TestResult`laridan qurilgan qisqartirilgan natija.
/// </summary>
internal sealed class GetStudentResultQueryHandler : IRequestHandler<GetStudentResultQuery, Result<GetStudentResultResult?>>
{
    private const string NoteUz = "Bu natija tashxis emas — hozirgi holatingiz surati.";
    private const int MaxTopStrengths = 3;
    private const int MaxCareerFields = 3;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IAppSettings _appSettings;

    public GetStudentResultQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _appSettings = appSettings;
    }

    public async Task<Result<GetStudentResultResult?>> Handle(GetStudentResultQuery request, CancellationToken cancellationToken)
    {
        if (!_appSettings.ShowResultToStudent)
        {
            return Result.Failure<GetStudentResultResult?>(new Error(ProblemCodes.Forbidden, "O'quvchiga natija ko'rsatish hozircha o'chirilgan."));
        }

        var now = _dateTime.UtcNow;

        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<GetStudentResultResult?>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        if (assessment.ExpiresAt <= now)
        {
            return Result.Failure<GetStudentResultResult?>(new Error(ProblemCodes.SessionExpired, "Sessiyaning amal qilish muddati tugagan."));
        }

        if (assessment.Status != AssessmentStatus.Analyzed)
        {
            // 202 — tahlil hali tayyor emas (`docs/07` 1.9-bo'lim). AI navbati hozircha
            // ulanmagan (`NoOpJobQueue`, P18'gacha) — bu holat amalda HAR DOIM shu bo'ladi.
            return Result.Success<GetStudentResultResult?>(null);
        }

        var testResults = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestResults).Where(r => r.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);

        var mbtiCode = testResults.FirstOrDefault(r => r.TestCode == "MBTI16")?.ResultCode;
        var riasecCode = testResults.FirstOrDefault(r => r.TestCode == "RIASEC")?.ResultCode;

        string typeName = "";
        string shortDescription = "";
        IReadOnlyList<string> topStrengths = [];

        if (!string.IsNullOrEmpty(mbtiCode))
        {
            var typeCatalogEntry = await _executor.FirstOrDefaultAsync(
                _context.AsNoTracking(_context.TypeCatalog).Where(t => t.Code == mbtiCode),
                cancellationToken).ConfigureAwait(false);

            if (typeCatalogEntry is not null)
            {
                typeName = typeCatalogEntry.NameUz;
                shortDescription = typeCatalogEntry.ShortDescriptionUz;
                topStrengths = typeCatalogEntry.Strengths.Take(MaxTopStrengths).ToList();
            }
        }

        var careerFields = await ResolveCareerFieldsAsync(riasecCode, cancellationToken).ConfigureAwait(false);

        var result = new GetStudentResultResult(
            PersonalityType: mbtiCode ?? "",
            TypeName: typeName,
            ShortDescription: shortDescription,
            TopStrengths: topStrengths,
            CareerFields: careerFields,
            Note: NoteUz);

        return Result.Success<GetStudentResultResult?>(result);
    }

    /// <summary>
    /// `docs/03` §4.3: `CareerMap.HollandCode` 2 harfli kalit. O'quvchining 3 harfli Holland
    /// kodi (`docs/03` §4.2 misoli: `IRA`) 15 mumkin bo'lgan juftlikdan faqat ba'zilarini
    /// qamraydi (seed'da 18 ta — barcha tartib bo'yicha 9 juftlik) — shu sabab ANIQ mos
    /// kelmasligi mumkin bo'lgan ikkinchi/uchinchi harf juftligini emas, o'quvchining TOP-3
    /// harfining IKKALASI ham (tartibsiz) shu 2 harfli kodda uchraydigan yozuvlarni tanlaydi
    /// (`RelevanceOrder` bo'yicha, 3 tagacha). Kod topilmasa (masalan `RIASEC` bu sessiyada
    /// yo'q) — bo'sh ro'yxat, xato emas (kasb yo'nalishlari ixtiyoriy ma'lumot).
    /// PM'ga savol: bu moslash mantiqi (aniq talqin yo'qligi sababli) tasdiqlanishi kerak.
    /// </summary>
    private async Task<IReadOnlyList<string>> ResolveCareerFieldsAsync(string? riasecCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(riasecCode))
        {
            return [];
        }

        var codeLetters = riasecCode.ToCharArray();

        var careerMapEntries = await _executor.ToListAsync(
            _context.AsNoTracking(_context.CareerMap).OrderBy(c => c.RelevanceOrder),
            cancellationToken).ConfigureAwait(false);

        return careerMapEntries
            .Where(c => c.HollandCode.All(letter => codeLetters.Contains(letter)))
            .OrderBy(c => c.RelevanceOrder)
            .Select(c => c.FieldNameUz)
            .Distinct()
            .Take(MaxCareerFields)
            .ToList();
    }
}

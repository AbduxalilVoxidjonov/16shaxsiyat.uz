using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Admin.Students.List;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Admin.Students.Export;

/// <summary>
/// `docs/07` 3.2-bo'lim, `prompts/27`. Handler QUERY bo'lsa ham (`docs/06` 4-bo'lim: "Query'lar
/// READ-ONLY") atayin bitta yon ta'sirga ega — audit yozuvi (`docs/08` §8: "Export.StudentsDownloaded").
/// Bu naqsh `GetSchoolInfoQueryHandler`dagi bilan bir xil (havola ochilishi hisoblagichi) —
/// FAIL-OPEN: audit yozib bo'lmasa ham, eksport fayli baribir qaytadi (pastdagi izohga qarang).
///
/// **Xotira strategiyasi (`prompts/27` MAXSUS DIQQAT #2):** filtrlangan o'quvchilar <see cref="BatchSize"/>
/// (500) talik BO'LAKLARDA `Skip`/`Take` bilan DB'dan o'qiladi (`Id` bo'yicha barqaror tartib —
/// `ListStudentsQueryHandler`dagi sahifalash bilan bir xil DB-darajasidagi yondashuv, ADR-11).
/// Har bo'lak uchun maktab nomi/oxirgi sessiya/tip nomi FAQAT o'sha bo'lakdagi o'quvchilar
/// bo'yicha (≤500 qator) alohida so'rov bilan to'ldiriladi — `ListStudentsQueryHandler`dagi
/// SAHIFA hajmidagi enrichment bilan bir xil naqsh, faqat "sahifa" o'rniga "bo'lak". Butun
/// filtrlangan natija hech qachon bitta `ToListAsync`ga yig'ilmaydi. `IExcelExporter.ExportAsync`
/// bu bo'laklarni `IAsyncEnumerable` orqali BITTA-BITTA qatorga aylantirib qabul qiladi —
/// `ClosedXML`ning o'zi yakuniy `.xlsx`ni xotirada quradi (formatning o'zi shart qiladi,
/// haqiqiy "streaming yozuvchi" kutubxona emas), lekin DB'dan BIR YO'LA butun jadvalni
/// yuklash — bu loyihada ikki marta xato bo'lgan aynan shu joy (`docs/06` §8, P14 qarori) —
/// oldini olinadi.
/// </summary>
internal sealed class ExportStudentsQueryHandler : IRequestHandler<ExportStudentsQuery, Result<AdminStudentsExportResult>>
{
    private const int BatchSize = 500;
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string SheetName = "O'quvchilar";

    private static readonly IReadOnlyList<string> Columns =
    [
        "FISH", "Maktab", "Viloyat/Tuman", "Sinf", "Jins", "Yosh", "Telefon", "Ota-ona telefoni",
        "Sana", "Holat", "Tip", "Tip nomi", "Yetuklik", "Aktivlik indeksi", "Aktivlik darajasi",
        "Holland kodi", "Ishonchlilik", "O'qish uchun izoh",
    ];

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IExcelExporter _excelExporter;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly ILogger<ExportStudentsQueryHandler> _logger;

    public ExportStudentsQueryHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IExcelExporter excelExporter,
        IDateTime dateTime,
        IIpHasher ipHasher,
        ILogger<ExportStudentsQueryHandler> logger)
    {
        _context = context;
        _executor = executor;
        _excelExporter = excelExporter;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _logger = logger;
    }

    public async Task<Result<AdminStudentsExportResult>> Handle(ExportStudentsQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        // AYNAN `ListStudentsQueryHandler` ishlatadigan filtr (`prompts/27` MAXSUS DIQQAT #1).
        // `OrderBy(Id)` — sahifalash emas, faqat `Skip`/`Take` bo'laklashning barqaror (stable)
        // bo'lishi uchun (aks holda ikkita ketma-ket `Skip` chaqiruvi bir xil qatorni ikki marta
        // yoki hech qachon qaytarmasligi kafolatlanmaydi).
        var filteredQuery = AdminStudentFilterBuilder.Apply(
            _context,
            _context.AsNoTracking(_context.Students),
            request.SchoolId,
            request.Grade,
            request.Status,
            request.NeedsAttention,
            request.PersonalityType,
            request.ActivityLevel,
            request.From,
            request.To,
            request.Search)
            .OrderBy(s => s.Id);

        var rowCount = 0;
        var typeNameCache = new Dictionary<string, string>(StringComparer.Ordinal);

        var content = await _excelExporter.ExportAsync(
            SheetName,
            Columns,
            BuildRowsAsync(filteredQuery, typeNameCache, now, count => rowCount = count, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var fileName = $"oquvchilar-{now:yyyy-MM-dd}.xlsx";

        // Audit (`docs/08` §8) — FAIL-OPEN: `GetSchoolInfoQueryHandler`dagi bilan bir xil sabab —
        // telemetriya/audit yozuvi muvaffaqiyatsiz bo'lsa ham, admin allaqachon tayyor bo'lgan
        // faylni olishi kerak (audit nosozligi mahsulot oqimini to'xtatmasligi kerak).
        //
        // Shaxsiy ma'lumot audit'ga YOZILMAYDI (`prompts/27` MAXSUS DIQQAT #3): `search` matni
        // o'zi ism bo'lishi mumkin — shu sabab faqat `HasSearch: true/false` yoziladi, matnning
        // o'zi ham, xeshi ham EMAS (audit yozuvi qidiruv so'zini keyinroq tiklash uchun ishlatilmaydi,
        // faqat "filtr qidiruv bilan qo'llanganmi" faktini qayd etadi).
        try
        {
            _context.Add(AuditLog.Create(
                AuditActions.ExportStudentsDownloaded,
                now,
                request.AdminUserId,
                entityType: "StudentExport",
                afterJson: AuditSnapshot.Serialize(new
                {
                    RowCount = rowCount,
                    Filter = new
                    {
                        request.SchoolId,
                        request.Grade,
                        request.Status,
                        request.NeedsAttention,
                        request.PersonalityType,
                        request.ActivityLevel,
                        request.From,
                        request.To,
                        HasSearch = !string.IsNullOrWhiteSpace(request.Search),
                    },
                }),
                ipHash: _ipHasher.Hash(request.IpAddress),
                userAgent: request.UserAgent));

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "O'quvchilar eksporti audit yozuvini saqlab bo'lmadi — fayl baribir qaytariladi (rowCount={RowCount}).", rowCount);
        }

        return Result.Success(new AdminStudentsExportResult(content, fileName, ExcelContentType));
    }

    private async IAsyncEnumerable<IReadOnlyList<ExcelCellValue>> BuildRowsAsync(
        IQueryable<Student> filteredQuery,
        Dictionary<string, string> typeNameCache,
        DateTimeOffset now,
        Action<int> reportRowCount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rowCount = 0;
        var offset = 0;

        while (true)
        {
            var batch = await _executor.ToListAsync(
                filteredQuery.Skip(offset).Take(BatchSize),
                cancellationToken).ConfigureAwait(false);

            if (batch.Count == 0)
            {
                break;
            }

            offset += batch.Count;

            var schoolIds = batch.Select(s => s.SchoolId).Distinct().ToList();
            var schools = await _executor.ToListAsync(
                _context.AsNoTracking(_context.Schools)
                    .Where(s => schoolIds.Contains(s.Id))
                    .Select(s => new { s.Id, s.Name, s.Region, s.District }),
                cancellationToken).ConfigureAwait(false);
            var schoolById = schools.ToDictionary(s => s.Id, s => s);

            var studentIds = batch.Select(s => s.Id).ToList();
            var assessments = await _executor.ToListAsync(
                _context.AsNoTracking(_context.Assessments).Where(a => studentIds.Contains(a.StudentId)),
                cancellationToken).ConfigureAwait(false);
            var latestByStudentId = assessments
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).First());

            var codesNeeded = batch
                .Select(s => s.LastPersonalityType)
                .Where(code => !string.IsNullOrEmpty(code) && !typeNameCache.ContainsKey(code))
                .Distinct()
                .ToList();
            if (codesNeeded.Count > 0)
            {
                var entries = await _executor.ToListAsync(
                    _context.AsNoTracking(_context.TypeCatalog)
                        .Where(t => codesNeeded.Contains(t.Code))
                        .Select(t => new { t.Code, t.NameUz }),
                    cancellationToken).ConfigureAwait(false);
                foreach (var entry in entries)
                {
                    typeNameCache[entry.Code] = entry.NameUz;
                }
            }

            foreach (var student in batch)
            {
                latestByStudentId.TryGetValue(student.Id, out var latestAssessment);
                schoolById.TryGetValue(student.SchoolId, out var school);

                rowCount++;
                yield return BuildRow(student, school?.Name, school?.Region, school?.District, latestAssessment, typeNameCache, now);
            }
        }

        reportRowCount(rowCount);
    }

    private static IReadOnlyList<ExcelCellValue> BuildRow(
        Student student,
        string? schoolName,
        string? region,
        string? district,
        Assessment? latestAssessment,
        IReadOnlyDictionary<string, string> typeNameCache,
        DateTimeOffset now)
    {
        var grade = student.ClassLetter is null ? student.Grade.ToString() : $"{student.Grade}-{student.ClassLetter}";
        var typeName = student.LastPersonalityType is not null && typeNameCache.TryGetValue(student.LastPersonalityType, out var name) ? name : null;

        return
        [
            ExcelCellValue.Of(student.FullName),
            ExcelCellValue.Of(schoolName ?? "?"),
            ExcelCellValue.Of($"{region ?? "?"}/{district ?? "?"}"),
            ExcelCellValue.Of(grade),
            ExcelCellValue.Of(AdminEnumLabels.Gender(student.Gender)),
            ExcelCellValue.Of((double)AgeCalculator.CalculateAge(student.BirthDate, now)),
            ExcelCellValue.Of(student.Phone.Value),
            ExcelCellValue.Of(student.ParentPhone?.Value),
            ExcelCellValue.Of(student.LastAssessmentAt),
            ExcelCellValue.Of(latestAssessment is null ? null : AdminEnumLabels.AssessmentStatus(latestAssessment.Status)),
            ExcelCellValue.Of(student.LastPersonalityType),
            ExcelCellValue.Of(typeName),
            ExcelCellValue.Of(student.LastMaturityIndex),
            ExcelCellValue.Of(student.LastActivityIndex),
            ExcelCellValue.Of(student.LastActivityLevel is null ? null : AdminEnumLabels.ActivityLevel(student.LastActivityLevel.Value)),
            ExcelCellValue.Of(student.LastHollandCode),
            ExcelCellValue.Of(latestAssessment?.ReliabilityFlag is null ? null : AdminEnumLabels.ReliabilityFlag(latestAssessment.ReliabilityFlag.Value)),
            ExcelCellValue.Of(BuildRemark(student.NeedsAttention, latestAssessment?.ReliabilityFlag)),
        ];
    }

    /// <summary>
    /// "O'qish uchun izoh" ustuni — `Student`da alohida izoh maydoni yo'q, shu sabab
    /// e'tibor/ishonchlilik bayroqlaridan qisqa avtomatik izoh yig'iladi (PM'ga savol: kelajakda
    /// admin qo'lda izoh qo'sha oladigan alohida maydon kerakmi?).
    /// </summary>
    private static string? BuildRemark(bool needsAttention, ReliabilityFlag? reliabilityFlag)
    {
        var parts = new List<string>();
        if (needsAttention)
        {
            parts.Add("E'tiborga muhtoj");
        }

        if (reliabilityFlag == ReliabilityFlag.Unreliable)
        {
            parts.Add("Ishonchlilik past — natija ehtiyot bilan talqin qilinsin");
        }
        else if (reliabilityFlag == ReliabilityFlag.Questionable)
        {
            parts.Add("Ishonchlilik shubhali");
        }

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }
}

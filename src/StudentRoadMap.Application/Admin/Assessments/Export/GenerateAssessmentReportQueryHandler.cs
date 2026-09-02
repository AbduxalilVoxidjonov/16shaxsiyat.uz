using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.Export;

/// <summary>
/// `docs/07` 3.3-bo'lim, `prompts/27` vazifa #2. Read-only — `AsNoTracking`. `StudentProfileMapping`
/// (`Admin.Students`, `internal` — bir xil assembly ichida ochiq) qayta ishlatiladi: `TestResult`/
/// `AiAnalysis` → DTO o'girish mantig'i `GetAssessmentByIdQueryHandler`/`GetStudentByIdQueryHandler`
/// bilan bir xil, uchinchi marta yozilmaydi.
///
/// **AI hali yo'q holati (`prompts/27` MAXSUS DIQQAT #4):** `AiAnalysis` `null` bo'lishi mumkin
/// (P16-P18 hali ulanmagan yoki tahlil hali navbatda) — bu XATO EMAS, oddiy holat. `IPdfExporter`
/// buni bo'sh bo'lim emas, "tahlil hali tayyor emas" izohi bilan ko'rsatadi (pastga qarang).
/// </summary>
internal sealed class GenerateAssessmentReportQueryHandler : IRequestHandler<GenerateAssessmentReportQuery, Result<AdminAssessmentReportFileDto>>
{
    private const string PdfContentType = "application/pdf";

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IPdfExporter _pdfExporter;

    public GenerateAssessmentReportQueryHandler(
        IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IPdfExporter pdfExporter)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _pdfExporter = pdfExporter;
    }

    public async Task<Result<AdminAssessmentReportFileDto>> Handle(GenerateAssessmentReportQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments).Where(a => a.Id == request.AssessmentId),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return Result.Failure<AdminAssessmentReportFileDto>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var student = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Students).Where(s => s.Id == assessment.StudentId),
            cancellationToken).ConfigureAwait(false);

        if (student is null)
        {
            // FK yaxlitligi buzilmagan bo'lsa bo'lishi mumkin emas — himoya sifatida.
            return Result.Failure<AdminAssessmentReportFileDto>(new Error(ProblemCodes.NotFound, "O'quvchi topilmadi."));
        }

        var school = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Schools).Where(s => s.Id == student.SchoolId),
            cancellationToken).ConfigureAwait(false);

        var testResults = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestResults).Where(r => r.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);
        var results = await StudentProfileMapping.BuildTestResultsAsync(testResults, _context, _executor, cancellationToken).ConfigureAwait(false);

        var aiAnalyses = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiAnalyses).Where(a => a.AssessmentId == assessment.Id),
            cancellationToken).ConfigureAwait(false);
        var currentAiAnalysis = aiAnalyses.FirstOrDefault(a => a.IsCurrent);
        var aiAnalysisDto = StudentProfileMapping.BuildAiAnalysis(currentAiAnalysis);

        var now = _dateTime.UtcNow;

        var reportData = new AssessmentReportData(
            assessment.Id,
            student.FullName,
            school?.Name ?? "?",
            student.Grade,
            student.ClassLetter,
            AgeCalculator.CalculateAge(student.BirthDate, now),
            student.Gender.ToString(),
            assessment.StartedAt,
            assessment.CompletedAt,
            assessment.ReliabilityScore,
            assessment.ReliabilityFlag?.ToString(),
            results,
            aiAnalysisDto,
            now);

        var pdfBytes = _pdfExporter.GenerateAssessmentReport(reportData);

        var fileName = $"hisobot-{assessment.Id:N}-{now:yyyy-MM-dd}.pdf";

        return Result.Success(new AdminAssessmentReportFileDto(pdfBytes, fileName, PdfContentType));
    }
}

using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.GetById;

/// <summary>
/// `docs/07` 3.3-bo'lim. Read-only — `AsNoTracking`. `StudentProfileMapping`
/// (`Admin.Students`, `internal` — bir xil assembly ichida ochiq) qayta ishlatiladi: bir xil
/// `TestResult`/`AiAnalysis` → JSON o'girish mantig'ini ikki joyda takrorlamaslik uchun
/// (`GetStudentByIdQueryHandler`dagi bilan bir xil).
///
/// <para>
/// **N+1 yo'q — jami 4 ta so'rov, sessiya tarkibi qanchalik katta bo'lsa ham:**
/// <list type="number">
///   <item>sarlavha — `assessments` + `students`/`schools`/`assessment_programs` ga UCHTA
///   `LEFT JOIN` (bitta proyeksiya, sikl ichida qo'shimcha so'rov YO'Q; `LEFT` — chunki
///   o'quvchi/maktab soft-delete qilingan bo'lsa ham sessiya detali ochilishi kerak);</item>
///   <item>`tests[]` — `assessment_tests` + `test_definitions` ga BITTA `JOIN`, `display_order`
///   bo'yicha saralangan (har test uchun alohida "nomini olib kel" so'rovi YO'Q);</item>
///   <item>`test_results` (mavjud);</item>
///   <item>`ai_analyses` (mavjud).</item>
/// </list>
/// `StudentProfileMapping.BuildTestResultsAsync` ichida `type_catalog`/`career_map` uchun
/// yana bir nechta so'rov bo'lishi mumkin, ammo ular ham TEST SONIGA bog'liq emas (4 ta
/// tizim bloki uchun qat'iy).
/// </para>
/// </summary>
internal sealed class GetAssessmentByIdQueryHandler : IRequestHandler<GetAssessmentByIdQuery, Result<AdminAssessmentDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetAssessmentByIdQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminAssessmentDetailDto>> Handle(GetAssessmentByIdQuery request, CancellationToken cancellationToken)
    {
        var headerQuery =
            from a in _context.AsNoTracking(_context.Assessments)
            where a.Id == request.Id
            join s in _context.Students on a.StudentId equals s.Id into studentJoin
            from s in studentJoin.DefaultIfEmpty()
            join sc in _context.Schools on a.SchoolId equals sc.Id into schoolJoin
            from sc in schoolJoin.DefaultIfEmpty()
            join p in _context.AssessmentPrograms on a.ProgramId equals p.Id into programJoin
            from p in programJoin.DefaultIfEmpty()
            select new
            {
                a.Id,
                a.Status,
                a.StartedAt,
                a.CompletedAt,
                a.TotalDurationSeconds,
                a.ReliabilityScore,
                a.ReliabilityFlag,
                a.StudentId,
                StudentFullName = s != null ? s.FullName : null,
                a.SchoolId,
                SchoolName = sc != null ? sc.Name : null,
                a.ProgramId,
                ProgramNameUz = p != null ? p.NameUz : null,
            };

        var header = await _executor.FirstOrDefaultAsync(headerQuery, cancellationToken).ConfigureAwait(false);

        if (header is null)
        {
            return Result.Failure<AdminAssessmentDetailDto>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
        }

        var testRows = await _executor.ToListAsync(
            from t in _context.AsNoTracking(_context.AssessmentTests)
            where t.AssessmentId == request.Id
            join d in _context.TestDefinitions on t.TestDefinitionId equals d.Id
            orderby t.DisplayOrder
            select new
            {
                t.TestDefinitionId,
                d.Code,
                d.NameUz,
                d.ScoringMode,
                d.Kind,
                t.Status,
                t.TotalCount,
                t.AnsweredCount,
            },
            cancellationToken).ConfigureAwait(false);

        var tests = testRows
            .Select(t => new AdminAssessmentTestItemDto(
                t.TestDefinitionId,
                t.Code,
                t.NameUz,
                t.ScoringMode.ToString(),
                t.Status.ToString(),
                t.TotalCount,
                t.AnsweredCount))
            .ToList();

        // `hasPersonalityBattery` — `PersonalityBattery` DOMEN qoidasidan (`Kind == Standard &&
        // ScoringMode == Scored`), metodika KODI ro'yxatidan EMAS. Mijoz shu bayroqqa qarab AI
        // tahlili chaqiruvini ko'rsatadi: so'rovnoma-only sessiyada AI tahlili tayyorlanmaydi
        // (`CompleteSessionCommandHandler` `Survey` bloklarini tahlildan chiqarib tashlaydi),
        // shu sabab "Tahlilni ishga tushirish" tugmasi u yerda chalg'ituvchi bo'lardi.
        // Bir xil hisob o'quvchi profilida ham bor (`AdminLatestAssessmentDto`).
        var hasPersonalityBattery = testRows.Any(t => PersonalityBattery.Includes(t.Kind, t.ScoringMode));

        var testResults = await _executor.ToListAsync(
            _context.AsNoTracking(_context.TestResults).Where(r => r.AssessmentId == header.Id),
            cancellationToken).ConfigureAwait(false);

        var results = await StudentProfileMapping.BuildTestResultsAsync(testResults, _context, _executor, cancellationToken).ConfigureAwait(false);

        var aiAnalyses = await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiAnalyses).Where(a => a.AssessmentId == header.Id),
            cancellationToken).ConfigureAwait(false);

        var currentAiAnalysis = aiAnalyses.FirstOrDefault(a => a.IsCurrent);

        var dto = new AdminAssessmentDetailDto(
            header.Id,
            results,
            StudentProfileMapping.BuildAiAnalysis(currentAiAnalysis),
            StudentProfileMapping.BuildAiHistory(aiAnalyses),
            header.Status.ToString(),
            header.StartedAt,
            header.CompletedAt,
            // Yakunlanmagan sessiyada `null` — `0` EMAS (`docs/06` qarorlar jurnali, 2026-09-02).
            header.TotalDurationSeconds.HasValue ? header.TotalDurationSeconds.Value / 60 : null,
            header.ReliabilityScore,
            header.ReliabilityFlag?.ToString(),
            header.StudentFullName is null ? null : new AdminAssessmentStudentRefDto(header.StudentId, header.StudentFullName),
            header.SchoolName is null ? null : new AdminAssessmentSchoolRefDto(header.SchoolId, header.SchoolName),
            header.ProgramNameUz is null ? null : new AdminAssessmentProgramRefDto(header.ProgramId, header.ProgramNameUz),
            tests,
            hasPersonalityBattery);

        return Result.Success(dto);
    }
}

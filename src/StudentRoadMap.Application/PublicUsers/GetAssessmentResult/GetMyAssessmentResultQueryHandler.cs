using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.GetStudentResult;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.GetAssessmentResult;

/// <summary>
/// Kabinetdagi natija. Proyeksiyaning O'ZI `StudentResultBuilder` orqali — ya'ni
/// `GET /api/public/sessions/result` bilan BIR XIL mantiq (nusxa yo'q).
///
/// Maktab oqimidan IKKI farqi bor va ikkalasi ham ATAYLAB:
///
/// 1. **Egalik** — `X-Session-Token` emas, `students.public_user_id`. Mos kelmasa `404`.
/// 2. **`ExpiresAt` TEKSHIRILMAYDI.** Maktab oqimida sessiya tokeni 7 kun yashaydi va
///    muddati o'tgach `410 SESSION_EXPIRED` beriladi — bu tokenning xavfsizlik chegarasi.
///    Kabinetda esa egalik doimiy (JWT + `public_user_id`), natija esa arxiv: 7 kundan
///    keyin o'z natijasini ko'ra olmaslik shaxsiy kabinetning ma'nosini yo'qotardi.
/// </summary>
internal sealed class GetMyAssessmentResultQueryHandler : IRequestHandler<GetMyAssessmentResultQuery, Result<GetStudentResultResult?>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IAppSettings _appSettings;
    private readonly StudentResultBuilder _resultBuilder;

    public GetMyAssessmentResultQueryHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IAppSettings appSettings,
        StudentResultBuilder resultBuilder)
    {
        _context = context;
        _executor = executor;
        _appSettings = appSettings;
        _resultBuilder = resultBuilder;
    }

    public async Task<Result<GetStudentResultResult?>> Handle(GetMyAssessmentResultQuery request, CancellationToken cancellationToken)
    {
        var assessment = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Assessments)
                .Where(a => a.Id == request.AssessmentId)
                .Select(a => new { a.Id, a.StudentId, a.SchoolId, a.Status }),
            cancellationToken).ConfigureAwait(false);

        if (assessment is null)
        {
            return NotFound();
        }

        var isOwner = await _executor.AnyAsync(
            _context.AsNoTracking(_context.Students)
                .Where(s => s.Id == assessment.StudentId && s.PublicUserId == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (!isOwner)
        {
            // Begona sessiya — `403` EMAS, `404`: mavjudligini oshkor qilmaydi (query izohi).
            return NotFound();
        }

        var school = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Schools).Where(s => s.Id == assessment.SchoolId),
            cancellationToken).ConfigureAwait(false);

        if (school is null || !ShowResultPolicy.IsAllowed(_appSettings.ShowResultToStudent, school))
        {
            return Result.Failure<GetStudentResultResult?>(new Error(ProblemCodes.Forbidden, ShowResultPolicy.ForbiddenMessageUz));
        }

        if (assessment.Status != AssessmentStatus.Analyzed)
        {
            // 202 — tahlil hali tayyor emas (maktab oqimidagi bilan bir xil semantika).
            return Result.Success<GetStudentResultResult?>(null);
        }

        var result = await _resultBuilder.BuildAsync(assessment.Id, cancellationToken).ConfigureAwait(false);

        return Result.Success<GetStudentResultResult?>(result);
    }

    private static Result<GetStudentResultResult?> NotFound() =>
        Result.Failure<GetStudentResultResult?>(new Error(ProblemCodes.NotFound, "Sessiya topilmadi."));
}

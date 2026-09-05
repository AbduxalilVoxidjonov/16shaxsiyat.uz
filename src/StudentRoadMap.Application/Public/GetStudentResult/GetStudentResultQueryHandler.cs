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
/// - `403 FORBIDDEN` — natija ko'rsatish o'chirilgan. **P47dan buyon bu IKKI bayroqning
///   birlashmasi** (`ShowResultPolicy`): GLOBAL `App:ShowResultToStudent` (kill-switch) VA
///   sessiya tegishli bo'lgan MAKONning `School.ShowResultToStudent`i. Global bayroq
///   birinchi tekshiriladi — u `false` bo'lsa DB'ga umuman murojaat qilinmaydi (avvalgi
///   xatti-harakat saqlangan); makon bayrog'i esa sessiya topilgandan keyin.
/// - `202` (`Result.Success(null)`, kontroller `Accepted()`ga aylantiradi) — sessiya hali
///   `Analyzed` holatiga yetmagan.
/// - `200` — `Analyzed` holatida, `StudentResultBuilder` qurgan qisqartirilgan natija.
/// </summary>
internal sealed class GetStudentResultQueryHandler : IRequestHandler<GetStudentResultQuery, Result<GetStudentResultResult?>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IAppSettings _appSettings;
    private readonly StudentResultBuilder _resultBuilder;

    public GetStudentResultQueryHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IAppSettings appSettings,
        StudentResultBuilder resultBuilder)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _appSettings = appSettings;
        _resultBuilder = resultBuilder;
    }

    public async Task<Result<GetStudentResultResult?>> Handle(GetStudentResultQuery request, CancellationToken cancellationToken)
    {
        // GLOBAL kill-switch — boshqa hech narsani tekshirmasdan, ENG BIRINCHI (DB'ga
        // bekorga murojaat qilinmaydi).
        if (!_appSettings.ShowResultToStudent)
        {
            return Forbidden();
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

        // MAKON bayrog'i — maktab natijani odatda psixolog orqali beradi (`false`), ommaviy
        // makon esa foydalanuvchiga to'g'ridan-to'g'ri ko'rsatadi (`true`).
        var school = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Schools).Where(s => s.Id == assessment.SchoolId),
            cancellationToken).ConfigureAwait(false);

        if (school is null || !ShowResultPolicy.IsAllowed(_appSettings.ShowResultToStudent, school))
        {
            return Forbidden();
        }

        if (assessment.Status != AssessmentStatus.Analyzed)
        {
            // 202 — tahlil hali tayyor emas (`docs/07` 1.9-bo'lim).
            return Result.Success<GetStudentResultResult?>(null);
        }

        var result = await _resultBuilder.BuildAsync(assessment.Id, cancellationToken).ConfigureAwait(false);

        return Result.Success<GetStudentResultResult?>(result);
    }

    private static Result<GetStudentResultResult?> Forbidden() =>
        Result.Failure<GetStudentResultResult?>(new Error(ProblemCodes.Forbidden, ShowResultPolicy.ForbiddenMessageUz));
}

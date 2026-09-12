using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.GetStudentProfile;

/// <summary>
/// Saqlangan anketani qaytaradi. `Student` `PublicUserId` bo'yicha qidiriladi — bitta akkaunt
/// → bitta profil (`ux_students_public_user`), shu sabab makon (`SchoolId`) bo'yicha
/// qo'shimcha filtr shart emas. Read-only (`AsNoTracking`).
///
/// `ConsentCurrent` — `PublicConsent.CurrentVersion` bilan solishtiriladi (versiya
/// `StartPublicSessionCommandHandler` bilan BIR manbadan; ikkita nusxa bo'lsa "anketa rozilik
/// so'ramadi, server esa talab qildi" nomuvofiqligi chiqardi).
/// </summary>
internal sealed class GetMyStudentProfileQueryHandler : IRequestHandler<GetMyStudentProfileQuery, Result<MyStudentProfileDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public GetMyStudentProfileQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result<MyStudentProfileDto>> Handle(GetMyStudentProfileQuery request, CancellationToken cancellationToken)
    {
        // Global filtr (`DeletedAt == null`) o'chirilgan akkauntni yashiradi — `GetMyProfileQueryHandler` bilan bir xil.
        var user = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.PublicUsers).Where(u => u.Id == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<MyStudentProfileDto>(new Error(ProblemCodes.PublicUserDeleted, "Bu akkaunt mavjud emas."));
        }

        var student = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Students).Where(s => s.PublicUserId == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(_dateTime.UtcNow.UtcDateTime);
        var registrationForm = await RegistrationFormResolver.GetGlobalDefinitionAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        // Xaritalash `PUT /api/me/profile` bilan umumiy (`MyStudentProfileMapper`).
        return Result.Success(MyStudentProfileMapper.ToDto(user, student, today, registrationForm));
    }
}

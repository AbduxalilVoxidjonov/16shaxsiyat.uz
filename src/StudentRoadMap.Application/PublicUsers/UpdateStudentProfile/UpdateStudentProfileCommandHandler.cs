using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Application.PublicUsers.GetStudentProfile;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.UpdateStudentProfile;

/// <summary>
/// Anketani saqlaydi — `docs/07` §5.1b. Oqim:
/// 1. Foydalanuvchi mavjudmi (o'chirilgan akkaunt global filtr bilan yashiringan);
/// 2. Ommaviy makon topiladi — yo'q bo'lsa `PUBLIC_SPACE_NOT_CONFIGURED`. Makon FAOL EMASligi
///    bu yerda TEKSHIRILMAYDI (sessiya handleridan farqi): foydalanuvchi o'z ma'lumotini
///    to'g'rilashi test o'tkazish emas — makon vaqtincha to'xtatilganda ham ruxsat beriladi;
/// 3. `Student` (`SchoolId`, `PublicUserId`) bo'yicha; majburiylik `RequireFields`;
/// 4. Bor → `ApplyChanges`; yo'q → `CreateStudent` + `Add`. **`Assessment` yaratilmaydi**,
///    kunlik hisoblagich oshirilmaydi, dastur tanlanmaydi;
/// 5. `SaveChangesAsync` → yangilangan `MyStudentProfileDto`.
/// </summary>
internal sealed class UpdateStudentProfileCommandHandler : IRequestHandler<UpdateStudentProfileCommand, Result<MyStudentProfileDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public UpdateStudentProfileCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result<MyStudentProfileDto>> Handle(UpdateStudentProfileCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var user = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.PublicUsers).Where(u => u.Id == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<MyStudentProfileDto>(new Error(ProblemCodes.PublicUserDeleted, "Bu akkaunt mavjud emas."));
        }

        var space = await PublicStudentProfile.FindPublicSpaceAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<MyStudentProfileDto>(PublicStudentProfile.PublicSpaceNotConfigured());
        }

        var existing = await PublicStudentProfile
            .FindStudentAsync(_context, _executor, space.Id, request.PublicUserId, cancellationToken)
            .ConfigureAwait(false);

        var errors = PublicStudentProfile.RequireFields(request, existing, today);
        if (errors.Count > 0)
        {
            return Result.Failure<MyStudentProfileDto>(PublicStudentProfile.ValidationFailure(errors));
        }

        Student student;

        if (existing is not null)
        {
            var applyResult = PublicStudentProfile.ApplyChanges(request, existing, now);
            if (applyResult.IsFailure)
            {
                return Result.Failure<MyStudentProfileDto>(applyResult.Error);
            }

            student = existing;
        }
        else
        {
            var createResult = PublicStudentProfile.CreateStudent(request, space.Id, request.PublicUserId, now);
            if (createResult.IsFailure)
            {
                return Result.Failure<MyStudentProfileDto>(createResult.Error);
            }

            student = createResult.Value;
            _context.Add(student);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(MyStudentProfileMapper.ToDto(user, student, today));
    }
}

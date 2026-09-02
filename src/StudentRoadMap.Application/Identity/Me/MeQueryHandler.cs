using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.Me;

internal sealed class MeQueryHandler : IRequestHandler<MeQuery, Result<AdminUserDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public MeQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<AdminUserDto>> Handle(MeQuery request, CancellationToken cancellationToken)
    {
        var user = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.AdminUsers).Where(u => u.Id == request.AdminUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<AdminUserDto>(new Error(ProblemCodes.NotFound, "Foydalanuvchi topilmadi."));
        }

        return Result.Success(new AdminUserDto(user.Id, user.Username, user.Email, user.FullName, user.Role.ToString(), user.TotpEnabled));
    }
}

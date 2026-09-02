using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.ChangePassword;

/// <summary>`docs/13-auth-va-jwt.md` MAXSUS DIQQAT 7-band: parol o'zgarganda BARCHA refresh tokenlar bekor qilinadi.</summary>
internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IPasswordHasher passwordHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var user = await _executor.FirstOrDefaultAsync(
            _context.AdminUsers.Where(u => u.Id == request.AdminUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Foydalanuvchi topilmadi."));
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(new Error(ProblemCodes.Unauthorized, "Joriy parol noto'g'ri."));
        }

        user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword), now);

        var activeTokens = await _executor.ToListAsync(
            _context.RefreshTokens.Where(t => t.AdminUserId == user.Id && t.RevokedAt == null),
            cancellationToken).ConfigureAwait(false);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        _context.Add(AuditLog.Create(AuditActions.AuthPasswordChanged, now, user.Id));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

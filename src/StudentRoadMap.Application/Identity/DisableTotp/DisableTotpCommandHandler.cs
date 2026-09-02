using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Identity.DisableTotp;

internal sealed class DisableTotpCommandHandler : IRequestHandler<DisableTotpCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IPasswordHasher _passwordHasher;

    public DisableTotpCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IPasswordHasher passwordHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(DisableTotpCommand request, CancellationToken cancellationToken)
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

        if (!user.TotpEnabled)
        {
            return Result.Failure(new Error(ProblemCodes.TotpNotEnabled, "TOTP yoqilmagan."));
        }

        user.DisableTotp(now);

        var backupCodes = await _executor.ToListAsync(
            _context.AdminTotpBackupCodes.Where(c => c.AdminUserId == user.Id),
            cancellationToken).ConfigureAwait(false);

        foreach (var backupCode in backupCodes)
        {
            _context.Remove(backupCode);
        }

        _context.Add(AuditLog.Create(AuditActions.AuthTotpDisabled, now, user.Id));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

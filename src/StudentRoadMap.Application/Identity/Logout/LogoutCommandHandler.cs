using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Common;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.Logout;

internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public LogoutCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return Result.Success();
        }

        var tokenHash = RefreshTokenHash.Compute(request.RawRefreshToken);

        var token = await _executor.FirstOrDefaultAsync(
            _context.RefreshTokens.Where(t => t.TokenHash == tokenHash),
            cancellationToken).ConfigureAwait(false);

        if (token is null || token.RevokedAt is not null)
        {
            return Result.Success();
        }

        token.Revoke(_dateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.Logout;

internal sealed class PublicLogoutCommandHandler : IRequestHandler<PublicLogoutCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;

    public PublicLogoutCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(PublicLogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return Result.Success();
        }

        var tokenHash = TokenHash.Compute(request.RawRefreshToken);

        var token = await _executor.FirstOrDefaultAsync(
            _context.PublicRefreshTokens.Where(t => t.TokenHash == tokenHash),
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

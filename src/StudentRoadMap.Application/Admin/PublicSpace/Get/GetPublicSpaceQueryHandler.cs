using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.Get;

/// <summary>
/// Read-only. Butun javob `PublicSpaceMapping.BuildAsync` da quriladi — mutatsiya handlerlari
/// ham AYNAN shu metoddan foydalanadi, ya'ni `GET` va mutatsiya javoblari hech qachon
/// ajralib ketmaydi.
/// </summary>
internal sealed class GetPublicSpaceQueryHandler : IRequestHandler<GetPublicSpaceQuery, Result<AdminPublicSpaceDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IAppSettings _appSettings;

    public GetPublicSpaceQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _appSettings = appSettings;
    }

    public async Task<Result<AdminPublicSpaceDto>> Handle(GetPublicSpaceQuery request, CancellationToken cancellationToken)
    {
        var space = await PublicSpaceMapping.FindAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<AdminPublicSpaceDto>(PublicSpaceMapping.NotConfigured());
        }

        var dto = await PublicSpaceMapping
            .BuildAsync(_context, _executor, _appSettings, space, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(dto);
    }
}

using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Delete;

/// <summary>`TestDefinition.RemoveScale` (Domain) `SYSTEM_TEST_LOCKED`/`SCALE_IN_USE` DomainException'ini bevosita ko'taradi — global middleware ushlaydi (`PublishProgramCommandHandler` uslubi).</summary>
internal sealed class DeleteTestScaleCommandHandler : IRequestHandler<DeleteTestScaleCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public DeleteTestScaleCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result> Handle(DeleteTestScaleCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var scale = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestScales).Where(s => s.Id == request.ScaleId),
            cancellationToken).ConfigureAwait(false);

        if (scale is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Shkala topilmadi."));
        }

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, scale.TestDefinitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Shkalaning anketasi topilmadi (ma'lumot izchilligi buzilgan).");

        test.RemoveScale(request.ScaleId, now);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogScaleChanged,
            now,
            request.AdminUserId,
            entityType: "TestScale",
            entityId: request.ScaleId,
            beforeJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, scale.Code }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        return Result.Success();
    }
}

using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Archive;

/// <summary>`TestDefinition.Archive` `Draft`/`Published`dan ishlaydi, `IsActive = false` ham qo'yadi — shu sabab ommaviy kesh bekor qilinadi.</summary>
internal sealed class ArchiveCatalogTestCommandHandler : IRequestHandler<ArchiveCatalogTestCommand, Result<CatalogTestDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public ArchiveCatalogTestCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogTestDetailDto>> Handle(ArchiveCatalogTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await _executor.FirstOrDefaultAsync(
            _context.TestDefinitions.Where(t => t.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        test.Archive(now);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogTestArchived,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            afterJson: AuditSnapshot.Serialize(new { test.Id, Status = test.Status.ToString() }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        var questionCount = await _executor.CountAsync(_context.AsNoTracking(_context.Questions).Where(q => q.TestDefinitionId == test.Id), cancellationToken).ConfigureAwait(false);
        var scaleCount = await _executor.CountAsync(_context.AsNoTracking(_context.TestScales).Where(s => s.TestDefinitionId == test.Id), cancellationToken).ConfigureAwait(false);
        var usedInProgramCount = await _executor.CountAsync(_context.AsNoTracking(_context.ProgramTests).Where(pt => pt.TestDefinitionId == test.Id), cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToDetailDto(test, questionCount, scaleCount, usedInProgramCount));
    }
}

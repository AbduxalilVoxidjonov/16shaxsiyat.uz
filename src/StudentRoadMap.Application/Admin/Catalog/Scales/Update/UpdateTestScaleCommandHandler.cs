using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Update;

internal sealed class UpdateTestScaleCommandHandler : IRequestHandler<UpdateTestScaleCommand, Result<CatalogScaleItemDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public UpdateTestScaleCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogScaleItemDto>> Handle(UpdateTestScaleCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var scale = await _executor.FirstOrDefaultAsync(
            _context.TestScales.Where(s => s.Id == request.ScaleId),
            cancellationToken).ConfigureAwait(false);

        if (scale is null)
        {
            return Result.Failure<CatalogScaleItemDto>(new Error(ProblemCodes.NotFound, "Shkala topilmadi."));
        }

        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == scale.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is not null && test.IsSystem)
        {
            return Result.Failure<CatalogScaleItemDto>(new Error(ProblemCodes.SystemTestLocked, "Tizim metodikasining shkalasi o'zgartirilmaydi."));
        }

        var bands = (request.InterpretationBands ?? []).Select(b => new InterpretationBand(b.From, b.To, b.Label)).ToList();
        scale.UpdateMetadata(request.NameUz, request.DescriptionUz, request.DisplayOrder);
        scale.UpdateInterpretationBands(bands);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogScaleChanged,
            now,
            request.AdminUserId,
            entityType: "TestScale",
            entityId: scale.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = scale.TestDefinitionId, scale.Code, scale.NameUz }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (test is not null)
        {
            _catalogCache.InvalidateTestDefinition(test.Id, test.Code);
        }

        var questionCount = await _executor.CountAsync(
            _context.AsNoTracking(_context.Questions).Where(q => q.TestDefinitionId == scale.TestDefinitionId && q.Scale == scale.Code),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToScaleDto(scale, questionCount));
    }
}

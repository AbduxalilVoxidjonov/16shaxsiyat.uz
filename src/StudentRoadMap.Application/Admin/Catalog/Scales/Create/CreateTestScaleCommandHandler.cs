using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Create;

/// <summary>
/// Nashr qilingan testga shkala qo'shilsa `TestDefinition.Version` oshadi (BR-9 ruhi) — bu
/// ommaviy keshdagi `CachedTestDefinitionDto.Version`ga ta'sir qiladi, shu sabab kesh har doim
/// bekor qilinadi ("ENG MUHIM" #2).
/// </summary>
internal sealed class CreateTestScaleCommandHandler : IRequestHandler<CreateTestScaleCommand, Result<CatalogScaleItemDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public CreateTestScaleCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogScaleItemDto>> Handle(CreateTestScaleCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.TestDefinitionId, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<CatalogScaleItemDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var displayOrder = request.DisplayOrder ?? test.Scales.Count;
        var bands = (request.InterpretationBands ?? []).Select(b => new InterpretationBand(b.From, b.To, b.Label)).ToList();

        var scale = TestScale.Create(Guid.NewGuid(), test.Id, request.Code, request.NameUz, displayOrder, bands, request.DescriptionUz);

        test.AddScale(scale, now);
        // ⚠️ QA topilmasi — `CreateTestQuestionCommandHandler` izohiga qarang: `test` so'rov
        // orqali tracked, aniq `Add()` bo'lmasa yangi `TestScale` "0 qator ta'sirlandi" bilan yiqiladi.
        _context.Add(scale);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogScaleChanged,
            now,
            request.AdminUserId,
            entityType: "TestScale",
            entityId: scale.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, scale.Code, scale.NameUz }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        return Result.Success(CatalogMapping.ToScaleDto(scale, questionCount: 0));
    }
}

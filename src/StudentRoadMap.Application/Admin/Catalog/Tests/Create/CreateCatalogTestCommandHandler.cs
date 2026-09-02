using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Create;

/// <summary>
/// Har doim `Kind = Custom`/`IsSystem = false`/`Status = Draft` (`docs/07` §3.4). Kod band bo'lsa
/// DB unique cheklovi (`ux_test_definitions_code`) `409 UNIQUE_CONSTRAINT_CONFLICT` beradi
/// (`CreateProgramCommandHandler` uslubi). `Scored` rejimida strategiya har doim `SUM` — admin
/// konstruktori boshqa strategiyani tanlay olmaydi (`docs/03` §6).
/// </summary>
internal sealed class CreateCatalogTestCommandHandler : IRequestHandler<CreateCatalogTestCommand, Result<CatalogTestDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public CreateCatalogTestCommandHandler(IAppDbContext context, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<CatalogTestDetailDto>> Handle(CreateCatalogTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var scoringMode = string.IsNullOrWhiteSpace(request.ScoringMode)
            ? TestScoringMode.Scored
            : Enum.Parse<TestScoringMode>(request.ScoringMode, ignoreCase: true);

        var test = TestDefinition.Create(
            Guid.NewGuid(),
            request.Code,
            request.NameUz,
            request.DisplayOrder ?? 0,
            request.EstimatedMinutes,
            scoringStrategyCode: scoringMode == TestScoringMode.Scored ? "SUM" : null,
            now,
            kind: TestKind.Custom,
            isSystem: false,
            pageSize: request.PageSize ?? 10,
            shuffleQuestions: request.ShuffleQuestions ?? false,
            descriptionUz: request.DescriptionUz,
            createdByAdminUserId: request.AdminUserId,
            scoringMode: scoringMode);

        _context.Add(test);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogTestCreated,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            afterJson: AuditSnapshot.Serialize(new { test.Id, test.Code, test.NameUz, ScoringMode = test.ScoringMode.ToString() }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToDetailDto(test, questionCount: 0, scaleCount: 0, usedInProgramCount: 0));
    }
}

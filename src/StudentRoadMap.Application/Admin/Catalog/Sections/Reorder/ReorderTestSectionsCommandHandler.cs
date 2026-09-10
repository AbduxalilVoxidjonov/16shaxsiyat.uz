using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Reorder;

/// <summary>
/// `TestDefinition.ReorderSections` (Domain) chaqiriladi — `SYSTEM_TEST_LOCKED`ni o'zi
/// tekshiradi. Noma'lum `Id` esa `ArgumentException` (500) o'rniga bu yerda OLDINDAN
/// `VALIDATION_ERROR` (400) sifatida qaytariladi (`ReorderTestQuestionsCommandHandler` uslubi).
/// </summary>
internal sealed class ReorderTestSectionsCommandHandler : IRequestHandler<ReorderTestSectionsCommand, Result<IReadOnlyList<CatalogSectionItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public ReorderTestSectionsCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<IReadOnlyList<CatalogSectionItemDto>>> Handle(ReorderTestSectionsCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.TestDefinitionId, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<IReadOnlyList<CatalogSectionItemDto>>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var sectionsById = test.Sections.ToDictionary(s => s.Id);

        foreach (var item in request.Items)
        {
            if (!sectionsById.ContainsKey(item.Id))
            {
                return Result.Failure<IReadOnlyList<CatalogSectionItemDto>>(new Error(
                    ProblemCodes.ValidationError,
                    $"'{item.Id}' bo'limi bu anketaga tegishli emas."));
            }
        }

        test.ReorderSections(request.Items.Select(i => (i.Id, i.DisplayOrder)).ToList(), now);

        _context.Add(AuditLog.Create(
            // `AuditActions`ga qo'shilmadi — `CreateTestSectionCommandHandler`dagi izohga qarang.
            "Catalog.SectionChanged",
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, ReorderedCount = request.Items.Count }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        var items = test.Sections.OrderBy(s => s.DisplayOrder).Select(CatalogMapping.ToSectionDto).ToList();

        return Result.Success<IReadOnlyList<CatalogSectionItemDto>>(items);
    }
}

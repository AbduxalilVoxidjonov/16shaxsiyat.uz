using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Delete;

/// <summary>`TestDefinition.RemoveSection` (Domain) `SYSTEM_TEST_LOCKED`/`SECTION_IN_USE` DomainException'ini bevosita ko'taradi (`DeleteTestScaleCommandHandler` uslubi).</summary>
internal sealed class DeleteTestSectionCommandHandler : IRequestHandler<DeleteTestSectionCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public DeleteTestSectionCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result> Handle(DeleteTestSectionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var section = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.QuestionSections).Where(s => s.Id == request.SectionId),
            cancellationToken).ConfigureAwait(false);

        if (section is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Bo'lim topilmadi."));
        }

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, section.TestDefinitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Bo'limning anketasi topilmadi (ma'lumot izchilligi buzilgan).");

        test.RemoveSection(request.SectionId, now);

        _context.Add(AuditLog.Create(
            // `AuditActions`ga qo'shilmadi — `CreateTestSectionCommandHandler`dagi izohga qarang.
            "Catalog.SectionChanged",
            now,
            request.AdminUserId,
            entityType: "QuestionSection",
            entityId: request.SectionId,
            beforeJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, section.Code }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        return Result.Success();
    }
}

using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Update;

internal sealed class UpdateTestSectionCommandHandler : IRequestHandler<UpdateTestSectionCommand, Result<CatalogSectionItemDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public UpdateTestSectionCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogSectionItemDto>> Handle(UpdateTestSectionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var section = await _executor.FirstOrDefaultAsync(
            _context.QuestionSections.Where(s => s.Id == request.SectionId),
            cancellationToken).ConfigureAwait(false);

        if (section is null)
        {
            return Result.Failure<CatalogSectionItemDto>(new Error(ProblemCodes.NotFound, "Bo'lim topilmadi."));
        }

        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == section.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is not null && test.IsSystem)
        {
            return Result.Failure<CatalogSectionItemDto>(new Error(ProblemCodes.SystemTestLocked, "Tizim metodikasining bo'limi o'zgartirilmaydi."));
        }

        // `QuestionSection.UpdateMetadata` (Domain) B-2ni TEKSHIRMAYDI (faqat `AddSection`
        // vaqtida) — bu yerda Application qatlamida qo'lda qo'riqlanadi (`docs/18` B-2).
        if (request.Visibility is not null && test is { ScoringMode: TestScoringMode.Scored })
        {
            return Result.Failure<CatalogSectionItemDto>(new Error(ProblemCodes.BranchingNotAllowedInScored, "Ko'rsatish sharti faqat 'Survey' rejimidagi anketalarda ishlatiladi."));
        }

        section.UpdateMetadata(request.TitleUz, request.DescriptionUz, request.Visibility);

        _context.Add(AuditLog.Create(
            // `AuditActions`ga qo'shilmadi — `CreateTestSectionCommandHandler`dagi izohga qarang.
            "Catalog.SectionChanged",
            now,
            request.AdminUserId,
            entityType: "QuestionSection",
            entityId: section.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = section.TestDefinitionId, section.Code, section.TitleUz }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (test is not null)
        {
            _catalogCache.InvalidateTestDefinition(test.Id, test.Code);
        }

        return Result.Success(CatalogMapping.ToSectionDto(section));
    }
}

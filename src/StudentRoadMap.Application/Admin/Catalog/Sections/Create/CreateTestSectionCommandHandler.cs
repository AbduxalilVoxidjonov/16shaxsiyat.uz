using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Create;

/// <summary>
/// `TestDefinition.AddSection` (Domain) `SYSTEM_TEST_LOCKED`/`SECTION_CODE_DUPLICATE`/
/// `BRANCHING_NOT_ALLOWED_IN_SCORED` DomainException'ini bevosita ko'taradi — global middleware
/// ushlaydi (`CreateTestQuestionCommandHandler` uslubi).
/// </summary>
internal sealed class CreateTestSectionCommandHandler : IRequestHandler<CreateTestSectionCommand, Result<CatalogSectionItemDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public CreateTestSectionCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogSectionItemDto>> Handle(CreateTestSectionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.TestDefinitionId, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<CatalogSectionItemDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var displayOrder = request.DisplayOrder ?? test.Sections.Count;

        var section = QuestionSection.Create(Guid.NewGuid(), test.Id, request.Code, request.TitleUz, displayOrder, request.DescriptionUz, request.Visibility);

        test.AddSection(section, now);

        // ⚠️ QA topilmasi — `CreateTestQuestionCommandHandler` izohiga qarang: `test` so'rov
        // orqali tracked, aniq `Add()` bo'lmasa yangi `QuestionSection` "0 qator ta'sirlandi"
        // bilan yiqiladi.
        _context.Add(section);

        _context.Add(AuditLog.Create(
            // Ataylab xom satr: `AuditActions` (`Domain/Identity/AuditActions.cs`) P52
            // vazifasining chegarasi bo'yicha o'zgartirilmadi (Domain qatlami "tayyor" deb
            // belgilangan) — `AuditLog.Create` `action`ni cheklanmagan `string` sifatida
            // qabul qiladi, shu sabab bu xavfsiz.
            "Catalog.SectionChanged",
            now,
            request.AdminUserId,
            entityType: "QuestionSection",
            entityId: section.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, section.Code, section.TitleUz }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        return Result.Success(CatalogMapping.ToSectionDto(section));
    }
}

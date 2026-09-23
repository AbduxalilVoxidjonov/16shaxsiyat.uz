using MediatR;
using StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Publish;

/// <summary>
/// `docs/03` §6.3 validatsiyasi (`CatalogPublishValidator`) + test nomi unikalligi (`docs/03`
/// §6.3: "test nomi va kodi unikal" — kodi DB unique cheklovi bilan yaratishda kafolatlangan,
/// nomi esa bu yerda tekshiriladi) BIRLASHTIRILGAN holda `issues` ro'yxatiga yig'iladi. Bo'sh
/// bo'lmasa `400 TEST_NOT_PUBLISHABLE` (`docs/07` §3.4 namunasi). Muvaffaqiyatli nashrdan keyin
/// ommaviy katalog keshi bekor qilinadi ("ENG MUHIM" #2, #3 — `Draft` sessiyaga tushmaydi,
/// lekin `Published`ga o'tgach DARHOL ko'rinishi ham muhim).
/// </summary>
internal sealed class PublishCatalogTestCommandHandler : IRequestHandler<PublishCatalogTestCommand, Result<CatalogTestDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public PublishCatalogTestCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogTestDetailDto>> Handle(PublishCatalogTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.Id, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var issues = CatalogPublishValidator.Validate(test).ToList();

        var duplicateName = await _executor.AnyAsync(
            _context.AsNoTracking(_context.TestDefinitions)
                .Where(t => t.Id != test.Id && t.NameUz == test.NameUz),
            cancellationToken).ConfigureAwait(false);
        if (duplicateName)
        {
            issues.Add(new PublishIssueDto("TEST_NAME_DUPLICATE", null, null, $"'{test.NameUz}' nomli anketa allaqachon mavjud."));
        }

        if (issues.Count > 0)
        {
            return Result.Failure<CatalogTestDetailDto>(new Error(
                ProblemCodes.TestNotPublishable,
                "Anketa nashr qilishga tayyor emas.",
                new Dictionary<string, object> { ["issues"] = issues }));
        }

        test.Publish(now);

        // 2026-09-23 (`docs/18` §9.7): test dasturi (bo'lsa) testga ergashadi — nashr qilingan test biriktirilgan joylarda darhol ochiladi.
        await TestPrograms.SyncIfExistsAsync(_context, _executor, test, now, cancellationToken).ConfigureAwait(false);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogTestPublished,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            afterJson: AuditSnapshot.Serialize(new { test.Id, Status = test.Status.ToString(), test.Version }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        var usedInProgramCount = await _executor.CountAsync(
            _context.AsNoTracking(_context.ProgramTests).Where(pt => pt.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToDetailDto(test, test.QuestionCount, test.Scales.Count, usedInProgramCount));
    }
}

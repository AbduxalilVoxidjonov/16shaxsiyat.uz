using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Duplicate;

/// <summary>
/// Manba testni (savollar+shkalalar bilan) TRACKED yuklaydi, so'ng `TestDefinition.Duplicate`
/// (Domain) savollar/shkalalarning TO'LIQ nusxasini yangi `Draft`/`Custom` agregatga ko'chiradi.
/// Nusxa `Draft` bo'lgani uchun ommaviy kesh bekor qilinmaydi ("ENG MUHIM" #3: `Draft`
/// hech qachon o'quvchi sessiyasiga tushmaydi — u umuman keshlanmaydi).
/// </summary>
internal sealed class DuplicateCatalogTestCommandHandler : IRequestHandler<DuplicateCatalogTestCommand, Result<CatalogTestDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public DuplicateCatalogTestCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<CatalogTestDetailDto>> Handle(DuplicateCatalogTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var source = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.Id, cancellationToken).ConfigureAwait(false);
        if (source is null)
        {
            return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var copy = source.Duplicate(Guid.NewGuid(), request.NewCode, now, request.AdminUserId);

        _context.Add(copy);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogTestDuplicated,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: copy.Id,
            beforeJson: AuditSnapshot.Serialize(new { SourceTestDefinitionId = source.Id, SourceCode = source.Code }),
            afterJson: AuditSnapshot.Serialize(new { copy.Id, copy.Code }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToDetailDto(copy, copy.QuestionCount, copy.Scales.Count, usedInProgramCount: 0));
    }
}

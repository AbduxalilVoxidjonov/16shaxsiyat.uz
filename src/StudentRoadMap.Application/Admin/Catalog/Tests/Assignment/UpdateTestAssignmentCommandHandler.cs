using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Admin.PublicSpace;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;

/// <summary>
/// Test biriktirmasini saqlaydi (2026-09-23 egasi qarori, `docs/18` §9.7):
/// <list type="number">
///   <item>test dasturi bo'lmasa — YARATADI (`TestPrograms.EnsureAsync`); so'rov "bo'sh"
///   bo'lsa (ommaviy emas, maktab yo'q, ommaviy makon yo'q, rejim `Full`) — yaratmaydi;</item>
///   <item>`IsPublic` → `ProgramVisibility.Public`, aks holda `Assigned`;</item>
///   <item>maktablar (`school_programs`, faqat `SchoolKind.School`) to'plamini TO'LIQ almashtiradi;</item>
///   <item>`RegistrationMode` — batareya invarianti domen darajasida
///   (`REGISTRATION_REQUIRED_FOR_BATTERY`, `400`);</item>
///   <item>`IsInPublicSpace` (`null` emas bo'lsa) — ommaviy makon biriktirmasi.</item>
/// </list>
///
/// **Draft test** — biriktirishga RUXSAT beriladi (oldindan tayyorlab qo'yish): test dasturi
/// holati testga ergashadi (`AssessmentProgram.SyncStateWithTest`), ya'ni test nashr
/// qilinmaguncha u hech bir maktabda/kabinetda ko'rinmaydi, nashr qilingan zahoti ochiladi.
/// **Arxivlangan test** — `409 TEST_ARCHIVED`.
/// </summary>
internal sealed class UpdateTestAssignmentCommandHandler : IRequestHandler<UpdateTestAssignmentCommand, Result<AdminTestAssignmentDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public UpdateTestAssignmentCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminTestAssignmentDto>> Handle(UpdateTestAssignmentCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure<AdminTestAssignmentDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        if (test.Status == TestDefinitionStatus.Archived)
        {
            return Result.Failure<AdminTestAssignmentDto>(TestAssignmentErrors.Archived());
        }

        var schoolIds = request.SchoolIds.Distinct().ToList();

        var schoolCheck = await TestAssignmentErrors.EnsureSchoolsExistAsync(_context, _executor, schoolIds, cancellationToken).ConfigureAwait(false);
        if (schoolCheck.IsFailure)
        {
            return Result.Failure<AdminTestAssignmentDto>(schoolCheck.Error);
        }

        Guid? publicSpaceId = null;
        if (request.IsInPublicSpace is not null)
        {
            var space = await PublicSpaceMapping.FindAsync(_context, _executor, cancellationToken).ConfigureAwait(false);
            if (space is null && request.IsInPublicSpace.Value)
            {
                return Result.Failure<AdminTestAssignmentDto>(PublicSpaceMapping.NotConfigured());
            }

            publicSpaceId = space?.Id;
        }

        var registrationMode = Enum.Parse<RegistrationMode>(request.RegistrationMode, ignoreCase: true);

        var existing = await TestPrograms.FindAsync(_context, _executor, test.Id, cancellationToken).ConfigureAwait(false);

        var isEmptyRequest = !request.IsPublic
            && schoolIds.Count == 0
            && request.IsInPublicSpace != true
            && registrationMode == RegistrationMode.Full;

        if (existing is null && isEmptyRequest)
        {
            // Biriktiradigan hech narsa yo'q — keraksiz test dasturi yaratilmaydi.
            var emptyDto = await TestAssignmentMapping.BuildAsync(_context, _executor, test, program: null, cancellationToken).ConfigureAwait(false);
            return Result.Success(emptyDto);
        }

        var before = existing is null
            ? null
            : await TestAssignmentMapping.BuildAsync(_context, _executor, test, existing, cancellationToken).ConfigureAwait(false);

        var (program, created) = await TestPrograms
            .EnsureAsync(_context, _executor, test, request.AdminUserId, now, cancellationToken)
            .ConfigureAwait(false);

        if (program.RegistrationMode != registrationMode)
        {
            // Batareyali testda `None` → `DomainException("REGISTRATION_REQUIRED_FOR_BATTERY")`,
            // `ExceptionHandlingMiddleware` `400`ga aylantiradi (tranzaksiya qaytariladi).
            program.SetRegistrationMode(registrationMode, test.IsPersonalityBattery, now);
        }

        program.SetPublic(request.IsPublic, now);

        await TestPrograms.ReplaceSchoolLinksAsync(_context, _executor, program.Id, schoolIds, now, cancellationToken).ConfigureAwait(false);

        if (publicSpaceId is not null)
        {
            await TestPrograms.SetPublicSpaceLinkAsync(
                _context, _executor, program.Id, publicSpaceId.Value, request.IsInPublicSpace!.Value, now, cancellationToken).ConfigureAwait(false);
        }

        _context.Add(AuditLog.Create(
            AuditActions.CatalogTestAssignmentUpdated,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            beforeJson: before is null ? null : AuditSnapshot.Serialize(Snapshot(before)),
            afterJson: AuditSnapshot.Serialize(new
            {
                request.IsPublic,
                SchoolIds = schoolIds,
                request.IsInPublicSpace,
                RegistrationMode = registrationMode.ToString(),
                ProgramCreated = created,
            }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await TestAssignmentMapping.BuildAsync(_context, _executor, test, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }

    private static object Snapshot(AdminTestAssignmentDto dto) => new
    {
        dto.IsPublic,
        dto.SchoolIds,
        dto.IsInPublicSpace,
        dto.RegistrationMode,
        dto.State,
    };
}

/// <summary>Test biriktirishning umumiy tekshiruvlari — test sahifasi, maktab formasi va ommaviy makon uchun bir xil.</summary>
internal static class TestAssignmentErrors
{
    public static Error Archived() => new(
        ProblemCodes.TestArchived,
        "Arxivlangan anketani biriktirib bo'lmaydi — avval yangi nusxa yarating.");

    /// <summary>Barcha ID'lar mavjud (o'chirilmagan) MAKTAB (`SchoolKind.School`) bo'lishi shart, aks holda `404`.</summary>
    public static async Task<Result> EnsureSchoolsExistAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IReadOnlyCollection<Guid> schoolIds,
        CancellationToken cancellationToken)
    {
        if (schoolIds.Count == 0)
        {
            return Result.Success();
        }

        var ids = schoolIds.ToList();
        var found = await executor.ToListAsync(
            context.AsNoTracking(context.Schools).SchoolsOnly().Where(s => ids.Contains(s.Id)).Select(s => s.Id),
            cancellationToken).ConfigureAwait(false);

        var missing = ids.Except(found).ToList();

        return missing.Count == 0
            ? Result.Success()
            : Result.Failure(new Error(
                ProblemCodes.NotFound,
                $"Maktab topilmadi: {string.Join(", ", missing)}.",
                new Dictionary<string, object> { ["schoolIds"] = missing }));
    }
}

using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.ReorderTests;

internal sealed class ReorderProgramTestsCommandHandler : IRequestHandler<ReorderProgramTestsCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public ReorderProgramTestsCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(ReorderProgramTestsCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AssessmentPrograms.Where(p => p.Id == request.ProgramId),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        // `program.Tests` navigatsiyasi shu nuqtada hali bo'sh (`IAppDbContext.AssessmentPrograms`
        // faqat `IQueryable` — `Include` Application qatlamida ishlatilmaydi, `docs/06` 3-bo'lim
        // ruhida). Fixup: `ProgramTests`ni SHU DbContext orqali TRACKED yuklaymiz —
        // `StartTestCommandHandler`dagi naqshga o'xshash — EF Core allaqachon tracked
        // `AssessmentProgram`/`ProgramTest` juftligini avtomatik bog'laydi (identity resolution),
        // shu sabab `program.Tests` shu so'rovdan keyin to'liq (`ReorderTests` domen metodi
        // tarkib bilan solishtira oladi).
        _ = await _executor.ToListAsync(
            _context.ProgramTests.Where(pt => pt.ProgramId == program.Id),
            cancellationToken).ConfigureAwait(false);

        program.ReorderTests(request.TestDefinitionIds, now);

        _context.Add(AuditLog.Create(
            AuditActions.ProgramTestsReordered,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            afterJson: AuditSnapshot.Serialize(new { program.Id, Order = request.TestDefinitionIds }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}

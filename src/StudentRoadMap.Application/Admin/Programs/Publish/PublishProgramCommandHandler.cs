using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.Publish;

/// <summary>`prompts/34` E15-band. Domen qo'riqchisi (`AssessmentProgram.Publish`): kamida bitta test bo'lmasa `400 PROGRAM_NOT_PUBLISHABLE`.</summary>
internal sealed class PublishProgramCommandHandler : IRequestHandler<PublishProgramCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public PublishProgramCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(PublishProgramCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AssessmentPrograms.Where(p => p.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        // Fixup: `program.Tests` bo'sh so'rov (Include'siz) bilan yuklanganda hali BO'SH —
        // `AssessmentProgram.Publish` "kamida bitta test" tekshiruvi (`_tests.Count`) shu
        // kolleksiyaga tayanadi, shu sabab haqiqiy tarkib avval SHU DbContext orqali
        // yuklanadi (`AddProgramTestCommandHandler`dagi izohga qarang — bu yerda faqat
        // O'QISH uchun, yangi yozuv qo'shilmaydi, shu sabab qo'shimcha `Add()` shart emas).
        var programTests = await _executor.ToListAsync(
            _context.ProgramTests.Where(pt => pt.ProgramId == program.Id),
            cancellationToken).ConfigureAwait(false);

        // P52: `REGISTRATION_REQUIRED_FOR_BATTERY` invariantining IKKINCHI nazorat nuqtasi
        // (`AssessmentProgram.Publish` izohi) — biriktirilgan testlar yuklanib, ilmiy
        // batareya bayrog'i domenga TAYYOR holda beriladi (`PersonalityBattery.ContainedIn`).
        var testDefinitionIds = programTests.Select(pt => pt.TestDefinitionId).ToList();
        var testDefinitions = await _executor.ToListAsync(
            _context.TestDefinitions.Where(t => testDefinitionIds.Contains(t.Id)),
            cancellationToken).ConfigureAwait(false);
        var hasPersonalityBattery = PersonalityBattery.ContainedIn(testDefinitions);

        program.Publish(now, hasPersonalityBattery);

        _context.Add(AuditLog.Create(
            AuditActions.ProgramPublished,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            afterJson: AuditSnapshot.Serialize(new { program.Id, Status = program.Status.ToString() }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}

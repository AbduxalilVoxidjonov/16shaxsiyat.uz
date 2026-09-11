using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.Update;

internal sealed class UpdateProgramCommandHandler : IRequestHandler<UpdateProgramCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public UpdateProgramCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(UpdateProgramCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AssessmentPrograms.Where(p => p.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        var before = AuditSnapshot.Serialize(new
        {
            program.Id, program.NameUz, Visibility = program.Visibility.ToString(), program.DisplayOrder,
            RegistrationMode = program.RegistrationMode.ToString(),
        });

        var visibility = Enum.Parse<ProgramVisibility>(request.Visibility, ignoreCase: true);
        var registrationMode = Enum.Parse<RegistrationMode>(request.RegistrationMode, ignoreCase: true);

        program.UpdateDetails(request.NameUz, request.DescriptionUz, request.DisplayOrder, now);
        if (program.Visibility != visibility)
        {
            program.SetVisibility(visibility, now);
        }

        if (program.RegistrationMode != registrationMode)
        {
            // P52: BIRINCHI nazorat nuqtasi (`AssessmentProgram.Publish` izohi) — biriktirilgan
            // testlar yuklanib, ilmiy batareya bayrog'i domenga TAYYOR holda beriladi.
            // Buzilsa `DomainException("REGISTRATION_REQUIRED_FOR_BATTERY")` OTILADI (ushlanmaydi —
            // `AddProgramTestCommandHandler`dagi `AddTest` bilan bir xil naqsh, `ExceptionHandlingMiddleware`
            // `400`ga aylantiradi, `ProblemCodes.HttpStatusByCode`).
            // `program.Tests` bu yerda ISHONCHSIZ — `program` Include'siz yuklangan
            // (`PublishProgramCommandHandler`dagi bilan bir xil sabab), shu sabab tarkib
            // ALOHIDA so'rov bilan o'qiladi.
            var currentProgramTests = await _executor.ToListAsync(
                _context.ProgramTests.Where(pt => pt.ProgramId == program.Id),
                cancellationToken).ConfigureAwait(false);
            var currentTestDefinitionIds = currentProgramTests.Select(pt => pt.TestDefinitionId).ToList();
            var currentTestDefinitions = await _executor.ToListAsync(
                _context.TestDefinitions.Where(t => currentTestDefinitionIds.Contains(t.Id)),
                cancellationToken).ConfigureAwait(false);
            var hasPersonalityBattery = PersonalityBattery.ContainedIn(currentTestDefinitions);

            program.SetRegistrationMode(registrationMode, hasPersonalityBattery, now);
        }

        var after = AuditSnapshot.Serialize(new
        {
            program.Id, program.NameUz, Visibility = program.Visibility.ToString(), program.DisplayOrder,
            RegistrationMode = program.RegistrationMode.ToString(),
        });

        _context.Add(AuditLog.Create(
            AuditActions.ProgramUpdated,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            beforeJson: before,
            afterJson: after,
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}

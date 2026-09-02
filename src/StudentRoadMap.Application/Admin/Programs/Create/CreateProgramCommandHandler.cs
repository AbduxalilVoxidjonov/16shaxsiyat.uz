using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.Create;

/// <summary>`prompts/34` E15-band. Kod band bo'lsa DB unique cheklovi (`ux_assessment_programs_code`) `409 UNIQUE_CONSTRAINT_CONFLICT` beradi (`CreateSchoolCommandHandler` uslubi).</summary>
internal sealed class CreateProgramCommandHandler : IRequestHandler<CreateProgramCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public CreateProgramCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(CreateProgramCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var visibility = Enum.Parse<ProgramVisibility>(request.Visibility, ignoreCase: true);

        var program = AssessmentProgram.Create(
            Guid.NewGuid(),
            request.Code,
            request.NameUz,
            now,
            displayOrder: request.DisplayOrder,
            kind: ProgramKind.Custom,
            visibility: visibility,
            descriptionUz: request.DescriptionUz,
            createdByAdminUserId: request.AdminUserId);

        _context.Add(program);

        _context.Add(AuditLog.Create(
            AuditActions.ProgramCreated,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            afterJson: AuditSnapshot.Serialize(new { program.Id, program.Code, program.NameUz, Visibility = program.Visibility.ToString() }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}

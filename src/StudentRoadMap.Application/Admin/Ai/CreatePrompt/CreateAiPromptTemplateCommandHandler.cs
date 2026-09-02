using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Ai.CreatePrompt;

internal sealed class CreateAiPromptTemplateCommandHandler : IRequestHandler<CreateAiPromptTemplateCommand, Result<AdminPromptTemplateDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public CreateAiPromptTemplateCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminPromptTemplateDto>> Handle(CreateAiPromptTemplateCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var duplicateVersion = await _executor.AnyAsync(
            _context.AsNoTracking(_context.PromptTemplates).Where(t => t.Key == request.Key && t.Version == request.Version),
            cancellationToken).ConfigureAwait(false);

        if (duplicateVersion)
        {
            return Result.Failure<AdminPromptTemplateDto>(new Error(
                ProblemCodes.ValidationError,
                $"'{request.Key}' uchun '{request.Version}' versiyasi allaqachon mavjud."));
        }

        // Bir xil `Key`dagi eski faol versiya bekor qilinadi — `docs/09` 10-bo'lim: "faol versiya bittasi".
        var previousActive = await _executor.ToListAsync(
            _context.PromptTemplates.Where(t => t.Key == request.Key && t.IsActive),
            cancellationToken).ConfigureAwait(false);

        foreach (var old in previousActive)
        {
            old.Deactivate();
        }

        var template = PromptTemplate.Create(Guid.NewGuid(), request.Key, request.Version, request.SystemText, request.UserText, request.JsonSchema, now);
        template.Activate();
        _context.Add(template);

        _context.Add(AuditLog.Create(
            AuditActions.AiConfigUpdated,
            now,
            request.AdminUserId,
            entityType: "PromptTemplate",
            entityId: template.Id,
            afterJson: AuditSnapshot.Serialize(new { template.Key, template.Version, IsActive = true }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = new AdminPromptTemplateDto(
            template.Id, template.Key, template.Version, template.SystemText, template.UserText, template.JsonSchema, template.IsActive, template.CreatedAt);

        return Result.Success(dto);
    }
}

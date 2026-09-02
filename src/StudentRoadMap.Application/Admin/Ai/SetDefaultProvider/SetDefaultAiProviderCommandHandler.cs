using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Ai.SetDefaultProvider;

/// <summary>
/// `docs/07` §3.5. `AiProviderConfig.MarkAsDefault` invarianti (`docs/04` §2.9: "faqat bitta faol
/// yozuvda `IsDefault = true`") — avvalgi standart (bo'lsa) `UnmarkAsDefault` bilan bekor qilinadi.
/// Faqat faol (`IsActive`) va kaliti bor providerni standart qilish mumkin (aks holda
/// `IAiProviderResolver.ResolveAsync(null, ...)` uni topa olmay xato berardi).
/// </summary>
internal sealed class SetDefaultAiProviderCommandHandler : IRequestHandler<SetDefaultAiProviderCommand, Result<AdminAiProviderDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IEncryptionService _encryptionService;
    private readonly IIpHasher _ipHasher;

    public SetDefaultAiProviderCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        IEncryptionService encryptionService,
        IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _encryptionService = encryptionService;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminAiProviderDto>> Handle(SetDefaultAiProviderCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var target = await _executor.FirstOrDefaultAsync(
            _context.AiProviderConfigs.Where(c => c.Provider == request.Provider),
            cancellationToken).ConfigureAwait(false);

        if (target is null)
        {
            return Result.Failure<AdminAiProviderDto>(new Error(ProblemCodes.NotFound, "Provider sozlanmagan."));
        }

        if (!target.IsActive || target.ApiKeyEncrypted is null)
        {
            return Result.Failure<AdminAiProviderDto>(new Error(
                ProblemCodes.ValidationError,
                "Faqat faol va kaliti kiritilgan providerni standart qilish mumkin."));
        }

        var previousDefault = await _executor.FirstOrDefaultAsync(
            _context.AiProviderConfigs.Where(c => c.IsDefault && c.Id != target.Id),
            cancellationToken).ConfigureAwait(false);
        previousDefault?.UnmarkAsDefault(now);

        target.MarkAsDefault(now);

        _context.Add(AuditLog.Create(
            AuditActions.AiConfigUpdated,
            now,
            request.AdminUserId,
            entityType: "AiProviderConfig",
            entityId: target.Id,
            afterJson: AuditSnapshot.Serialize(new { Provider = target.Provider.ToString(), IsDefault = true }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var maskedApiKey = ApiKeyMasker.Mask(_encryptionService.Decrypt(target.ApiKeyEncrypted));

        var dto = new AdminAiProviderDto(
            target.Provider,
            target.DisplayName,
            target.Model,
            target.BaseUrl,
            target.MaxOutputTokens,
            target.Temperature,
            target.IsDefault,
            target.IsActive,
            target.FallbackOrder,
            maskedApiKey,
            target.LastCheckedAt,
            target.LastCheckStatus);

        return Result.Success(dto);
    }
}

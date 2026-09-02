using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Ai.UpdateProvider;

/// <summary>
/// `docs/07` §3.5. Kalit shifrlanadi (`IEncryptionService`, `CLAUDE.md` 4-qoida) — DB'da xom
/// matn sifatida hech qachon saqlanmaydi. Audit: har doim `AiConfig.Updated`; kalit ALMASHGANDA
/// (yangi qiymat berilganda) QO'SHIMCHA `AiConfig.KeyChanged` — ikkalasida ham kalit QIYMATI yo'q.
/// </summary>
internal sealed class UpdateAiProviderCommandHandler : IRequestHandler<UpdateAiProviderCommand, Result<AdminAiProviderDto>>
{
    private static readonly IReadOnlyDictionary<AiProvider, string> DefaultDisplayNames = new Dictionary<AiProvider, string>
    {
        [AiProvider.Gemini] = "Google Gemini",
        [AiProvider.OpenAi] = "OpenAI",
        [AiProvider.Anthropic] = "Anthropic Claude",
    };

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IEncryptionService _encryptionService;
    private readonly IIpHasher _ipHasher;

    public UpdateAiProviderCommandHandler(
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

    public async Task<Result<AdminAiProviderDto>> Handle(UpdateAiProviderCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var config = await _executor.FirstOrDefaultAsync(
            _context.AiProviderConfigs.Where(c => c.Provider == request.Provider),
            cancellationToken).ConfigureAwait(false);

        var isNew = config is null;
        if (config is null)
        {
            config = AiProviderConfig.Create(
                Guid.NewGuid(),
                request.Provider,
                DefaultDisplayNames.GetValueOrDefault(request.Provider, request.Provider.ToString()),
                request.Model,
                now,
                request.MaxOutputTokens,
                request.Temperature,
                request.FallbackOrder);
            _context.Add(config);
        }

        config.UpdateSettings(config.DisplayName, request.Model, request.BaseUrl, request.MaxOutputTokens, request.Temperature, request.FallbackOrder, now);

        var keyChanged = false;
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            config.SetApiKey(_encryptionService.Encrypt(request.ApiKey), now);
            keyChanged = true;
        }

        if (request.IsActive && config.ApiKeyEncrypted is null)
        {
            return Result.Failure<AdminAiProviderDto>(new Error(
                ProblemCodes.ValidationError,
                "Kalit kiritilmagan providerni faollashtirib bo'lmaydi."));
        }

        if (request.IsActive)
        {
            config.Activate(now);
        }
        else
        {
            config.Deactivate(now);
        }

        _context.Add(AuditLog.Create(
            AuditActions.AiConfigUpdated,
            now,
            request.AdminUserId,
            entityType: "AiProviderConfig",
            entityId: config.Id,
            afterJson: AuditSnapshot.Serialize(new
            {
                Provider = config.Provider.ToString(),
                config.Model,
                config.MaxOutputTokens,
                config.Temperature,
                config.IsActive,
                config.FallbackOrder,
                IsNew = isNew,
            }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        if (keyChanged)
        {
            // Kalit QIYMATI bu yerda YO'Q — faqat "o'zgardi" fakti (`CLAUDE.md` qat'iy qoidasi).
            _context.Add(AuditLog.Create(
                AuditActions.AiConfigKeyChanged,
                now,
                request.AdminUserId,
                entityType: "AiProviderConfig",
                entityId: config.Id,
                afterJson: AuditSnapshot.Serialize(new { Provider = config.Provider.ToString() }),
                ipHash: _ipHasher.Hash(request.IpAddress),
                userAgent: request.UserAgent));
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var maskedApiKey = config.ApiKeyEncrypted is null ? null : ApiKeyMasker.Mask(_encryptionService.Decrypt(config.ApiKeyEncrypted));

        var dto = new AdminAiProviderDto(
            config.Provider,
            config.DisplayName,
            config.Model,
            config.BaseUrl,
            config.MaxOutputTokens,
            config.Temperature,
            config.IsDefault,
            config.IsActive,
            config.FallbackOrder,
            maskedApiKey,
            config.LastCheckedAt,
            config.LastCheckStatus);

        return Result.Success(dto);
    }
}

using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Ai;

/// <summary>
/// Superadmin kiritgan `AiProviderConfig` yozuvlaridan (kaliti bor va `IsActive` bo'lganlar)
/// haqiqiy `IAiAnalysisProvider` implementatsiyasini quradi — `docs/09-ai-analiz-moduli.md`
/// 2 va 7-bo'lim, `prompts/17` vazifa #2. Provider tanlovi hech qachon qattiq yozilmaydi
/// (`CLAUDE.md` — provider abstraksiyasi qoidasi): faqat shu klass orqali.
/// </summary>
public sealed class AiProviderResolver : IAiProviderResolver
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiProviderResolver(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _executor = executor;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<AiProviderInfo>> GetAvailableAsync(CancellationToken cancellationToken)
    {
        var configs = await LoadActiveConfigsAsync(cancellationToken).ConfigureAwait(false);

        return configs
            .OrderBy(c => c.FallbackOrder)
            .Select(c => new AiProviderInfo(c.Provider, c.DisplayName, c.Model, c.IsDefault, c.FallbackOrder))
            .ToList();
    }

    public async Task<IAiAnalysisProvider> ResolveAsync(AiProvider? requested, CancellationToken cancellationToken)
    {
        var configs = await LoadActiveConfigsAsync(cancellationToken).ConfigureAwait(false);

        var config = requested is { } provider
            ? configs.FirstOrDefault(c => c.Provider == provider)
            : configs.FirstOrDefault(c => c.IsDefault);

        if (config is null)
        {
            var message = requested is { } requestedProvider
                ? $"'{requestedProvider}' AI provayderi faol emas yoki kaliti kiritilmagan."
                : "Standart (default) AI provayder sozlanmagan yoki faol emas.";
            throw new InvalidOperationException(message);
        }

        return CreateProvider(config);
    }

    public async Task<IReadOnlyList<IAiAnalysisProvider>> GetFallbackChainAsync(AiProvider primary, CancellationToken cancellationToken)
    {
        var configs = await LoadActiveConfigsAsync(cancellationToken).ConfigureAwait(false);

        // `primary` bu ro'yxatdan chiqarib tashlanadi — u allaqachon "urinish 1" sifatida
        // sinalgan; bu yerdagi ro'yxat faqat "urinish 2/3" uchun (`docs/09` 7-bo'lim).
        return configs
            .Where(c => c.Provider != primary)
            .OrderBy(c => c.FallbackOrder)
            .Select(CreateProvider)
            .ToList();
    }

    private async Task<List<AiProviderConfig>> LoadActiveConfigsAsync(CancellationToken cancellationToken) =>
        await _executor.ToListAsync(
            _context.AsNoTracking(_context.AiProviderConfigs)
                .Where(c => c.IsActive && c.ApiKeyEncrypted != null),
            cancellationToken).ConfigureAwait(false);

    private IAiAnalysisProvider CreateProvider(AiProviderConfig config)
    {
        // `LoadActiveConfigsAsync` filtri (`ApiKeyEncrypted != null`) tufayli bu yerda har doim
        // qiymatga ega — `!` shu kafolatga tayanadi.
        var apiKey = _encryptionService.Decrypt(config.ApiKeyEncrypted!);

        return config.Provider switch
        {
            AiProvider.Gemini => new GeminiProvider(_httpClientFactory, apiKey, config.Model, config.BaseUrl),
            AiProvider.OpenAi => new OpenAiProvider(_httpClientFactory, apiKey, config.Model, config.BaseUrl),
            AiProvider.Anthropic => new AnthropicProvider(_httpClientFactory, apiKey, config.Model, config.BaseUrl),
            _ => throw new NotSupportedException($"Noma'lum AI provayder: {config.Provider}"),
        };
    }
}

using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Tests.Jobs.Testing;

/// <summary>
/// `AnalysisOrchestrator` testlari uchun — haqiqiy `AiProviderResolver` DB (`AiProviderConfig`)
/// va haqiqiy Gemini/OpenAi/Anthropic klasslariga bog'liq; bu yerda faqat orkestratorning
/// fallback zanjiri mantig'i (`docs/09` 7-bo'lim) tekshiriladi, shu sabab qo'lda boshqariladigan
/// soxta ro'yxat yetarli.
/// </summary>
internal sealed class FakeAiProviderResolver : IAiProviderResolver
{
    private readonly List<(IAiAnalysisProvider Provider, bool IsDefault, int FallbackOrder)> _providers = [];

    public FakeAiProviderResolver Add(IAiAnalysisProvider provider, bool isDefault = false, int fallbackOrder = 100)
    {
        _providers.Add((provider, isDefault, fallbackOrder));
        return this;
    }

    public Task<IReadOnlyList<AiProviderInfo>> GetAvailableAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AiProviderInfo>>(_providers
            .OrderBy(p => p.FallbackOrder)
            .Select(p => new AiProviderInfo(p.Provider.Kind, p.Provider.Kind.ToString(), "test-model", p.IsDefault, p.FallbackOrder))
            .ToList());

    public Task<IAiAnalysisProvider> ResolveAsync(AiProvider? requested, CancellationToken cancellationToken)
    {
        var match = requested is { } provider
            ? _providers.FirstOrDefault(p => p.Provider.Kind == provider)
            : _providers.FirstOrDefault(p => p.IsDefault);

        if (match.Provider is null)
        {
            throw new InvalidOperationException("FakeAiProviderResolver: mos provider topilmadi (haqiqiy AiProviderResolver'dagi bilan bir xil kontrakt).");
        }

        return Task.FromResult(match.Provider);
    }

    public Task<IReadOnlyList<IAiAnalysisProvider>> GetFallbackChainAsync(AiProvider primary, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<IAiAnalysisProvider>>(_providers
            .Where(p => p.Provider.Kind != primary)
            .OrderBy(p => p.FallbackOrder)
            .Select(p => p.Provider)
            .ToList());

    public Task<IAiAnalysisProvider> ResolveForTestAsync(AiProvider provider, CancellationToken cancellationToken) =>
        ResolveAsync(provider, cancellationToken);
}

using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Superadmin kiritgan `AiProviderConfig` yozuvlaridan (kaliti bor va faol bo'lganlar)
/// haqiqiy `IAiAnalysisProvider` implementatsiyasini tanlaydi — `docs/09-ai-analiz-moduli.md`
/// 2 va 7-bo'lim (retry/fallback zanjiri). Bu promptda (P16) implementatsiya YO'Q — faqat
/// shartnoma; haqiqiy resolver P17/P18'da qo'shiladi.
/// </summary>
public interface IAiProviderResolver
{
    /// <summary>Kaliti bor va faol barcha providerlarning yengil ro'yxati.</summary>
    Task<IReadOnlyList<AiProviderInfo>> GetAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// `requested` berilsa — aynan o'shani (faol va kaliti bo'lsa) qaytaradi; `null` bo'lsa
    /// `IsDefault = true` bo'lgan providerni qaytaradi.
    /// </summary>
    Task<IAiAnalysisProvider> ResolveAsync(AiProvider? requested, CancellationToken cancellationToken);

    /// <summary>
    /// `primary`dan boshlab `FallbackOrder` bo'yicha tartiblangan zanjir — 7-bo'limdagi
    /// "urinish 2/3" mantig'i shu ro'yxat bo'yicha ketma-ket sinaladi.
    /// </summary>
    Task<IReadOnlyList<IAiAnalysisProvider>> GetFallbackChainAsync(AiProvider primary, CancellationToken cancellationToken);

    /// <summary>
    /// P18 (`Application.Admin.Ai.TestProvider`, `docs/07` §3.5 "POST .../test"): superadmin
    /// hali `IsActive = false` (faollashtirishdan OLDIN) bo'lgan providerni ham sinab ko'ra olishi
    /// kerak — shu sabab `ResolveAsync`dan farqli o'laroq `IsActive`ni TEKSHIRMAYDI, faqat kalit
    /// kiritilganini (`ApiKeyEncrypted != null`) talab qiladi. Kalit yo'q/config topilmasa
    /// `InvalidOperationException`.
    /// </summary>
    Task<IAiAnalysisProvider> ResolveForTestAsync(AiProvider provider, CancellationToken cancellationToken);
}

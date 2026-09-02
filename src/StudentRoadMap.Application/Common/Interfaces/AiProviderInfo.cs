using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `IAiProviderResolver.GetAvailableAsync` elementi — kaliti bor va faol (`IsActive = true`)
/// `AiProviderConfig`larning yengil proyeksiyasi (`docs/09-ai-analiz-moduli.md` 2-bo'lim).
/// API kaliti (hatto shifrlangan holda ham) bu yerda YO'Q — faqat tanlov uchun kerakli maydonlar.
/// </summary>
/// <param name="Provider">Provider turi.</param>
/// <param name="DisplayName">Superadmin panelida ko'rsatiladigan nom.</param>
/// <param name="Model">Konfiguratsiyadagi model nomi.</param>
/// <param name="IsDefault">Standart provider — so'ralmagan holatda shu tanlanadi.</param>
/// <param name="FallbackOrder">Fallback zanjiridagi tartib — kichikroq qiymat oldinroq sinaladi.</param>
public sealed record AiProviderInfo(
    AiProvider Provider,
    string DisplayName,
    string Model,
    bool IsDefault,
    int FallbackOrder);

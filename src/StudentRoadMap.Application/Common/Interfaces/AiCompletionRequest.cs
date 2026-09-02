using System.Text.Json;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `IAiAnalysisProvider.CompleteJsonAsync` so'rovi — `docs/09-ai-analiz-moduli.md` 2-bo'lim.
/// `SystemText`/`UserText` — `PromptBuilder` tomonidan `prompt_templates`dan (yoki embedded
/// default'dan) qurilgan matnlar; `JsonSchema` — structured output uchun `AnalysisJsonSchema`.
/// </summary>
/// <param name="SystemText">Tizim (system) prompti.</param>
/// <param name="UserText">Foydalanuvchi (user) prompti — ichida `AnalysisInput` JSON'i bor.</param>
/// <param name="JsonSchema">Structured output sxemasi (provider bu bo'yicha javobni cheklaydi).</param>
/// <param name="Model">Model nomi — konfiguratsiyadan (`ai_provider_configs.model`), kodda qattiq yozilmaydi.</param>
/// <param name="MaxOutputTokens">Maksimal chiqish token soni.</param>
/// <param name="Temperature">Determinizm darajasi — standart `0.4` (`docs/09` 10-bo'lim).</param>
public sealed record AiCompletionRequest(
    string SystemText,
    string UserText,
    JsonDocument JsonSchema,
    string Model,
    int MaxOutputTokens,
    double Temperature);

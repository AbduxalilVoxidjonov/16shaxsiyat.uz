using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Admin.Ai;

/// <summary>
/// `GET /api/admin/ai/providers` elementi — `docs/07-api-shartnoma.md` §3.5. `MaskedApiKey` —
/// hech qachon to'liq kalit emas (masalan `AIza••••••7f2b`), kalit umuman kiritilmagan bo'lsa
/// `null`. `docs/07`da aniq JSON namuna YO'Q (faqat yo'l jadvali) — bu shakl shu promptda (P18)
/// belgilangan, PM'ga hisobotda ro'yxat qilingan.
/// </summary>
public sealed record AdminAiProviderDto(
    AiProvider Provider,
    string DisplayName,
    string Model,
    string? BaseUrl,
    int MaxOutputTokens,
    decimal Temperature,
    bool IsDefault,
    bool IsActive,
    int FallbackOrder,
    string? MaskedApiKey,
    DateTimeOffset? LastCheckedAt,
    string? LastCheckStatus);

/// <summary>`POST /api/admin/ai/providers/{provider}/test` javobi — `docs/07` §3.5: "{ ok, latencyMs, message }".</summary>
public sealed record AdminAiProviderTestResultDto(bool Ok, int? LatencyMs, string Message);

/// <summary>`GET /api/admin/ai/prompts` elementi.</summary>
public sealed record AdminPromptTemplateDto(
    Guid Id,
    string Key,
    string Version,
    string SystemText,
    string UserText,
    string JsonSchema,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>`GET /api/admin/ai/usage?from=&amp;to=` javobi — davr bo'yicha token/xarajat statistikasi (`docs/09-ai-analiz-moduli.md` 9-bo'lim).</summary>
public sealed record AdminAiUsageDto(
    int TotalCalls,
    int InputTokens,
    int OutputTokens,
    decimal? EstimatedCostUsd,
    IReadOnlyList<AdminAiUsageByProviderDto> ByProvider);

public sealed record AdminAiUsageByProviderDto(
    AiProvider Provider,
    int Calls,
    int InputTokens,
    int OutputTokens,
    decimal? EstimatedCostUsd);

/// <summary>`POST /api/admin/assessments/{id}/rerun-analysis` javobi — `docs/07` §3.3.</summary>
public sealed record RerunAnalysisResultDto(Guid AssessmentId, string Status);

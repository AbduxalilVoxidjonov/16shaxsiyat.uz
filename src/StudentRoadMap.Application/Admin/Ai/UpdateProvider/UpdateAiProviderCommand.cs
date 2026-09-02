using MediatR;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.UpdateProvider;

/// <summary>
/// `PUT /api/admin/ai/providers/{provider}` — `docs/07-api-shartnoma.md` §3.5:
/// `{ apiKey?, model, maxOutputTokens, temperature, isActive, fallbackOrder }`. Yozuv yo'q
/// bo'lsa YARATADI (upsert) — superadmin birinchi marta provider sozlashi shu yo'l bilan bo'ladi.
/// `ApiKey` faqat YOZISH-uchun (`write-only`) — bo'sh/`null` bo'lsa mavjud kalit O'ZGARMAYDI.
/// </summary>
public sealed record UpdateAiProviderCommand(
    AiProvider Provider,
    string Model,
    int MaxOutputTokens,
    decimal Temperature,
    bool IsActive,
    int FallbackOrder,
    string? ApiKey,
    string? BaseUrl,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminAiProviderDto>>;

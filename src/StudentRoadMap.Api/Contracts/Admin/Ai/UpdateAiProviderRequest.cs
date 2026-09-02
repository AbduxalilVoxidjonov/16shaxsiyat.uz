using StudentRoadMap.Application.Admin.Ai.UpdateProvider;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Api.Contracts.Admin.Ai;

/// <summary>`PUT /api/admin/ai/providers/{provider}` so'rov tanasi — `docs/07` §3.5: "{ apiKey?, model, maxOutputTokens, temperature, isActive, fallbackOrder }".</summary>
public sealed record UpdateAiProviderRequest(
    string? ApiKey,
    string Model,
    int MaxOutputTokens,
    decimal Temperature,
    bool IsActive,
    int FallbackOrder,
    string? BaseUrl)
{
    public UpdateAiProviderCommand ToCommand(AiProvider provider, Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(provider, Model, MaxOutputTokens, Temperature, IsActive, FallbackOrder, ApiKey, BaseUrl, adminUserId, ipAddress, userAgent);
}

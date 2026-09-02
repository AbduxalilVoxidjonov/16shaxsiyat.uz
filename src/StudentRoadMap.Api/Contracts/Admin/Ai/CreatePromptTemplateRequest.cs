using StudentRoadMap.Application.Admin.Ai.CreatePrompt;

namespace StudentRoadMap.Api.Contracts.Admin.Ai;

/// <summary>`POST /api/admin/ai/prompts` so'rov tanasi — `docs/07` §3.5: "Prompt shablon versiyalari".</summary>
public sealed record CreatePromptTemplateRequest(string Key, string Version, string SystemText, string UserText, string JsonSchema)
{
    public CreateAiPromptTemplateCommand ToCommand(Guid adminUserId, string? ipAddress, string? userAgent) =>
        new(Key, Version, SystemText, UserText, JsonSchema, adminUserId, ipAddress, userAgent);
}

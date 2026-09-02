using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.ListPrompts;

/// <summary>`GET /api/admin/ai/prompts` — `docs/07-api-shartnoma.md` §3.5: "Prompt shablon versiyalari".</summary>
public sealed record ListAiPromptsQuery : IRequest<Result<IReadOnlyList<AdminPromptTemplateDto>>>;

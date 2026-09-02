using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Ai.CreatePrompt;

/// <summary>
/// `POST /api/admin/ai/prompts` — `docs/07-api-shartnoma.md` §3.5: "Prompt shablon versiyalari"
/// (aniq JSON namuna YO'Q — PM'ga hisobotda qayd etilgan). Yaratilgan versiya DARHOL faollashadi
/// va bir xil `Key`dagi OLDINGI faol versiyani bekor qiladi (`docs/09-ai-analiz-moduli.md`
/// 10-bo'lim: "faol versiya bittasi") — `docs/07`da alohida "activate" yo'li YO'Q, shu sabab
/// yagona mavjud yo'l (`POST`) shu vazifani ham bajaradi deb talqin qilindi.
/// </summary>
public sealed record CreateAiPromptTemplateCommand(
    string Key,
    string Version,
    string SystemText,
    string UserText,
    string JsonSchema,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminPromptTemplateDto>>;

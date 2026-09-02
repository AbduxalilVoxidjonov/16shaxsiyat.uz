using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Ai;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Ai;
using StudentRoadMap.Application.Admin.Ai.CreatePrompt;
using StudentRoadMap.Application.Admin.Ai.GetUsage;
using StudentRoadMap.Application.Admin.Ai.ListPrompts;
using StudentRoadMap.Application.Admin.Ai.ListProviders;
using StudentRoadMap.Application.Admin.Ai.SetDefaultProvider;
using StudentRoadMap.Application.Admin.Ai.TestProvider;
using StudentRoadMap.Application.Admin.Ai.UpdateProvider;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// AI sozlamalari admin API'si — `docs/07-api-shartnoma.md` §3.5, P18 (`prompts/18` "qo'shimcha
/// ravishda" vazifasi, P28 UI shu endpoint'larga tayanadi). Faqat superadmin.
/// <para>
/// **Marshrut:** `docs/07`da har uch provider-doiraviy amal (`PUT`/`test`/`set-default`)
/// `{provider}` (enum nomi, masalan `Gemini`) bo'yicha — `AiProviderConfig.Provider` ustida
/// `ux_ai_provider_kind` unique indeksi bor, shu sabab provider turi tabiiy identifikator.
/// </para>
/// </summary>
[ApiController]
[Route("api/admin/ai")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class AiConfigController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public AiConfigController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`GET /api/admin/ai/providers` — kalitlar maskalangan.</summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminAiProviderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminAiProviderDto>>> ListProviders(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAiProvidersQuery(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`PUT /api/admin/ai/providers/{provider}` — yozuv yo'q bo'lsa yaratadi (upsert).</summary>
    [HttpPut("providers/{provider}")]
    [ProducesResponseType(typeof(AdminAiProviderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<AdminAiProviderDto>> UpdateProvider(
        AiProvider provider, [FromBody] UpdateAiProviderRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(provider, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/ai/providers/{provider}/test` — aloqa tekshiruvi.</summary>
    [HttpPost("providers/{provider}/test")]
    [ProducesResponseType(typeof(AdminAiProviderTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminAiProviderTestResultDto>> TestProvider(AiProvider provider, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new TestAiProviderCommand(provider, RequireAdminUserId()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/ai/providers/{provider}/set-default`.</summary>
    [HttpPost("providers/{provider}/set-default")]
    [ProducesResponseType(typeof(AdminAiProviderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<AdminAiProviderDto>> SetDefaultProvider(AiProvider provider, CancellationToken cancellationToken)
    {
        var command = new SetDefaultAiProviderCommand(provider, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/admin/ai/prompts`.</summary>
    [HttpGet("prompts")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminPromptTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminPromptTemplateDto>>> ListPrompts(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAiPromptsQuery(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/ai/prompts` — yangi versiya darhol faollashadi (bir xil `key`dagi eskisini bekor qiladi).</summary>
    [HttpPost("prompts")]
    [ProducesResponseType(typeof(AdminPromptTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<AdminPromptTemplateDto>> CreatePrompt([FromBody] CreatePromptTemplateRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(ListPrompts), null, result.Value);
    }

    /// <summary>`GET /api/admin/ai/usage?from=&amp;to=`.</summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(AdminAiUsageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAiUsageDto>> GetUsage(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAiUsageQuery(from, to), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}

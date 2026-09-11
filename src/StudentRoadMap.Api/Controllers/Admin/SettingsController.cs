using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Settings;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Settings.RegistrationForm;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Global sozlamalar admin API'si — `docs/07-api-shartnoma.md` §3.8. 2026-09-11/12,
/// egasining talabi: ro'yxatdan o'tish formasi endi HAR DASTURDA emas, "Sozlamalar" sahifasidan
/// GLOBAL boshqariladi (`docs/18-tarmoqlanuvchi-sorovnoma.md` §9.6). Faqat superadmin.
/// </summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class SettingsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public SettingsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>`GET /api/admin/settings/registration-form` — sozlama yo'q bo'lsa STANDART qaytadi (`null` emas).</summary>
    [HttpGet("registration-form")]
    [ProducesResponseType(typeof(RegistrationFormDefinitionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RegistrationFormDefinitionDto>> GetRegistrationForm(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRegistrationFormSettingsQuery(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`PUT /api/admin/settings/registration-form` — to'liq almashtirish, audit: `Settings.RegistrationFormUpdated`.</summary>
    [HttpPut("registration-form")]
    [ProducesResponseType(typeof(RegistrationFormDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<RegistrationFormDefinitionDto>> UpdateRegistrationForm(
        [FromBody] UpdateRegistrationFormSettingsRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Catalog;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Questions.Create;
using StudentRoadMap.Application.Admin.Catalog.Questions.Delete;
using StudentRoadMap.Application.Admin.Catalog.Questions.List;
using StudentRoadMap.Application.Admin.Catalog.Questions.Update;
using StudentRoadMap.Application.Admin.Catalog.Scales.Create;
using StudentRoadMap.Application.Admin.Catalog.Scales.Delete;
using StudentRoadMap.Application.Admin.Catalog.Scales.List;
using StudentRoadMap.Application.Admin.Catalog.Scales.Update;
using StudentRoadMap.Application.Admin.Catalog.Tests.Archive;
using StudentRoadMap.Application.Admin.Catalog.Tests.Create;
using StudentRoadMap.Application.Admin.Catalog.Tests.Delete;
using StudentRoadMap.Application.Admin.Catalog.Tests.Duplicate;
using StudentRoadMap.Application.Admin.Catalog.Tests.GetById;
using StudentRoadMap.Application.Admin.Catalog.Tests.List;
using StudentRoadMap.Application.Admin.Catalog.Tests.Preview;
using StudentRoadMap.Application.Admin.Catalog.Tests.Publish;
using StudentRoadMap.Application.Admin.Catalog.Tests.ToggleActive;
using StudentRoadMap.Application.Admin.Catalog.Tests.Update;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Api.Controllers.Admin;

/// <summary>
/// Test katalogi va anketa konstruktori admin API'si — `docs/07` §3.4 (P37,
/// `prompts/37-katalog-crud-backend.md`). Faqat superadmin. `TestScale` savol/shkala
/// endpoint'lari faqat `Custom` testlarda to'liq ochiq — tizim metodikasida `409
/// SYSTEM_TEST_LOCKED` (BR-8, `CLAUDE.md` 9a-qoida).
/// </summary>
[ApiController]
[Route("api/admin/catalog")]
[Authorize(Policy = JwtAuthenticationSetup.SuperAdminPolicy)]
[EnableRateLimiting(RateLimitSetup.AdminApi)]
public sealed class AssessmentCatalogController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public AssessmentCatalogController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    // --- Testlar --------------------------------------------------------------------------

    /// <summary>`GET /api/admin/catalog/tests` — `docs/07` §3.4: "Barchasi". Sahifalashsiz TO'LIQ massiv (katalog hajmi kichik).</summary>
    [HttpGet("tests")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogTestListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CatalogTestListItemDto>>> ListTests(
        [FromQuery] string? kind,
        [FromQuery] bool? isSystem,
        [FromQuery] string? status,
        [FromQuery] string? scoringMode,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListCatalogTestsQuery(kind, isSystem, status, scoringMode), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/admin/catalog/tests/{id}` — batafsil.</summary>
    [HttpGet("tests/{id:guid}")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> GetTestById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCatalogTestByIdQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests` — har doim `Custom`/`Draft`.</summary>
    [HttpPost("tests")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> CreateTest([FromBody] CreateCatalogTestRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(GetTestById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>`PUT /api/admin/catalog/tests/{id}` — `Code`/`Kind`/savollar o'zgarmaydi.</summary>
    [HttpPut("tests/{id:guid}")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> UpdateTest(Guid id, [FromBody] UpdateCatalogTestRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/publish` — `docs/03` §6.3 validatsiyasi yiqilsa `400 TEST_NOT_PUBLISHABLE` (`issues[]`).</summary>
    [HttpPost("tests/{id:guid}/publish")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PublishCatalogTestCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/duplicate` — nusxa doim `Draft`/`Custom`.</summary>
    [HttpPost("tests/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> Duplicate(Guid id, [FromBody] DuplicateCatalogTestRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(GetTestById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/toggle-active` — BR-10.</summary>
    [HttpPost("tests/{id:guid}/toggle-active")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> ToggleActive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ToggleCatalogTestActiveCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/archive` — BR-11.</summary>
    [HttpPost("tests/{id:guid}/archive")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ArchiveCatalogTestCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/catalog/tests/{id}` — faqat `Custom` + ishlatilmagan.</summary>
    [HttpDelete("tests/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteTest(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCatalogTestCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    /// <summary>`GET /api/admin/catalog/tests/{id}/preview` — o'quvchi ko'radigan ko'rinish.</summary>
    [HttpGet("tests/{id:guid}/preview")]
    [ProducesResponseType(typeof(CatalogTestPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CatalogTestPreviewDto>> Preview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCatalogTestPreviewQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    // --- Shkalalar (faqat Custom) -----------------------------------------------------------

    /// <summary>`GET /api/admin/catalog/tests/{id}/scales`.</summary>
    [HttpGet("tests/{id:guid}/scales")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogScaleItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<CatalogScaleItemDto>>> ListScales(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListTestScalesQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/scales` — tizim testida `409 SYSTEM_TEST_LOCKED`.</summary>
    [HttpPost("tests/{id:guid}/scales")]
    [ProducesResponseType(typeof(CatalogScaleItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogScaleItemDto>> CreateScale(Guid id, [FromBody] CreateTestScaleRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(ListScales), new { id }, result.Value);
    }

    /// <summary>`PUT /api/admin/catalog/scales/{scaleId}`.</summary>
    [HttpPut("scales/{scaleId:guid}")]
    [ProducesResponseType(typeof(CatalogScaleItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogScaleItemDto>> UpdateScale(Guid scaleId, [FromBody] UpdateTestScaleRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(scaleId, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/catalog/scales/{scaleId}` — savollari bo'lsa `409 SCALE_IN_USE`.</summary>
    [HttpDelete("scales/{scaleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteScale(Guid scaleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteTestScaleCommand(scaleId, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    // --- Savollar ---------------------------------------------------------------------------

    /// <summary>`GET /api/admin/catalog/tests/{id}/questions` — tizim testida ham ✅.</summary>
    [HttpGet("tests/{id:guid}/questions")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogQuestionItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<CatalogQuestionItemDto>>> ListQuestions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListTestQuestionsQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/questions` — tizim testida `409 SYSTEM_TEST_LOCKED`.</summary>
    [HttpPost("tests/{id:guid}/questions")]
    [ProducesResponseType(typeof(CatalogQuestionItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogQuestionItemDto>> CreateQuestion(Guid id, [FromBody] CreateTestQuestionRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(ListQuestions), new { id }, result.Value);
    }

    /// <summary>`PUT /api/admin/catalog/questions/{id}` — tizim savolida faqat `textUz`/`textRu`/`isActive` amalda ta'sir qiladi.</summary>
    [HttpPut("questions/{id:guid}")]
    [ProducesResponseType(typeof(CatalogQuestionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogQuestionItemDto>> UpdateQuestion(Guid id, [FromBody] UpdateTestQuestionRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/catalog/questions/{id}` — tizim testida `409 SYSTEM_TEST_LOCKED`.</summary>
    [HttpDelete("questions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteTestQuestionCommand(id, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/questions/reorder` — `[{id, displayOrder}]`, tizim testida ham ✅.</summary>
    [HttpPost("tests/{id:guid}/questions/reorder")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogQuestionItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<CatalogQuestionItemDto>>> ReorderQuestions(Guid id, [FromBody] ReorderTestQuestionsRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/questions/import` — `Custom` to'liq; tizim faqat matn yangilaydi. Natija doim `Draft` bo'lib qoladi (test allaqachon `Draft` bo'lmasa import buni o'zgartirmaydi).</summary>
    [HttpPost("tests/{id:guid}/questions/import")]
    [ProducesResponseType(typeof(CatalogTestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogTestDetailDto>> ImportQuestions(Guid id, [FromBody] ImportTestQuestionsRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}

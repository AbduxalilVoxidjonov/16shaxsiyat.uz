using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Auth;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Contracts.Admin.Catalog;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Admin.Catalog;
using StudentRoadMap.Application.Admin.Catalog.Excel;
using StudentRoadMap.Application.Admin.Catalog.Questions.Create;
using StudentRoadMap.Application.Admin.Catalog.Questions.Delete;
using StudentRoadMap.Application.Admin.Catalog.Questions.List;
using StudentRoadMap.Application.Admin.Catalog.Questions.Update;
using StudentRoadMap.Application.Admin.Catalog.Scales.Create;
using StudentRoadMap.Application.Admin.Catalog.Sections.Create;
using StudentRoadMap.Application.Admin.Catalog.Sections.Delete;
using StudentRoadMap.Application.Admin.Catalog.Sections.List;
using StudentRoadMap.Application.Admin.Catalog.Sections.Reorder;
using StudentRoadMap.Application.Admin.Catalog.Sections.Update;
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
using StudentRoadMap.Domain.Common;

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

    // --- Bo'limlar (faqat Custom, `docs/18` §5) ----------------------------------------------

    /// <summary>`GET /api/admin/catalog/tests/{id}/sections`.</summary>
    [HttpGet("tests/{id:guid}/sections")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogSectionItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<CatalogSectionItemDto>>> ListSections(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListTestSectionsQuery(id), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/sections` — tizim testida `409 SYSTEM_TEST_LOCKED`.</summary>
    [HttpPost("tests/{id:guid}/sections")]
    [ProducesResponseType(typeof(CatalogSectionItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogSectionItemDto>> CreateSection(Guid id, [FromBody] CreateTestSectionRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(ListSections), new { id }, result.Value);
    }

    /// <summary>`PUT /api/admin/catalog/sections/{sectionId}` — `code` o'zgarmaydi.</summary>
    [HttpPut("sections/{sectionId:guid}")]
    [ProducesResponseType(typeof(CatalogSectionItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<CatalogSectionItemDto>> UpdateSection(Guid sectionId, [FromBody] UpdateTestSectionRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(sectionId, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>`DELETE /api/admin/catalog/sections/{sectionId}` — savollari bo'lsa `409 SECTION_IN_USE`.</summary>
    [HttpDelete("sections/{sectionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteSection(Guid sectionId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteTestSectionCommand(sectionId, RequireAdminUserId(), ClientIp(), UserAgent()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error);
    }

    /// <summary>`POST /api/admin/catalog/tests/{id}/sections/reorder` — `[{id, displayOrder}]`.</summary>
    [HttpPost("tests/{id:guid}/sections/reorder")]
    [ProducesResponseType(typeof(IReadOnlyList<CatalogSectionItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<CatalogSectionItemDto>>> ReorderSections(Guid id, [FromBody] ReorderTestSectionsRequest request, CancellationToken cancellationToken)
    {
        var command = request.ToCommand(id, RequireAdminUserId(), ClientIp(), UserAgent());
        var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
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

    // --- Excel: shablon, eksport, o'qish (P39, `docs/07` §3.4) -----------------------------

    /// <summary>
    /// `GET /api/admin/catalog/tests/{id}/export.xlsx` — mavjud anketani Excel'ga chiqaradi.
    /// Tizim metodikasi uchun ham OCHIQ: bu o'qish amali, BR-8 faqat o'zgartirishni qulflaydi
    /// (`CLAUDE.md` 9a). Aynan shu — import uchun eng yaxshi namuna.
    /// </summary>
    [HttpGet("tests/{id:guid}/export.xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ExportTestExcel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ExportCatalogTestExcelQuery(id), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>`GET /api/admin/catalog/import-template.xlsx` — bo'sh shablon (ko'rsatma varag'i + bitta namunaviy qator).</summary>
    [HttpGet("import-template.xlsx")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportTemplateExcel(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCatalogExcelTemplateQuery(), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error);
        }

        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// `POST /api/admin/catalog/import/parse-excel` — `.xlsx` ni o'qib, MAVJUD JSON import
    /// sxemasidagi obyektni va topilgan xatolar ro'yxatini qaytaradi. HECH NARSA SAQLANMAYDI:
    /// oldindan ko'rish, validatsiya va yaratish frontend'dagi MAVJUD yo'l orqali ketadi, shu
    /// sabab ikkita alohida import mantiqi paydo bo'lmaydi va serverda vaqtinchalik holat
    /// saqlash kerak emas.
    ///
    /// <para>
    /// <b>Xavfsizlik.</b> `.xlsx` — ZIP arxiv, ya'ni himoyasiz yuklash endpointi butun API'ni
    /// bo'g'ib qo'yishi mumkin. <see cref="RequestSizeLimitAttribute"/> Kestrel darajasida
    /// tanani chegaralaydi (oshsa `413 PAYLOAD_TOO_LARGE` — P31 da qo'shilgan ishlov),
    /// pastdagi aniq tekshiruv esa multipart ichidagi FAYL qismini chegaralaydi. Qolgan
    /// tekshiruvlar (zip bomba, `.xls`/`.xlsm`, mazmun bo'yicha Open XML, qator soni)
    /// `CatalogExcelWorkbook.Parse` da.
    /// </para>
    /// </summary>
    [HttpPost("import/parse-excel")]
    [RequestSizeLimit(CatalogExcelLimits.MaxFileBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ParseCatalogExcelResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    public async Task<ActionResult<ParseCatalogExcelResultDto>> ParseImportExcel(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return this.ToProblem(new Error(ProblemCodes.ImportFileInvalid, "Fayl yuborilmadi."));
        }

        if (file.Length > CatalogExcelLimits.MaxFileBytes)
        {
            return PayloadTooLarge();
        }

        using var buffer = new MemoryStream();
        await using (var stream = file.OpenReadStream())
        {
            await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        var result = await _sender.Send(new ParseCatalogExcelQuery(buffer.ToArray()), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }

    /// <summary>
    /// `413` javobi — `ProblemDetailsSetup.PayloadTooLarge` kodi bilan, `ControllerResultExtensions.ToProblem`
    /// bilan AYNAN bir xil shaklda (P31: har bir xato `code` va `traceId` bilan, ichki tafsilotsiz).
    /// `ProblemCodes.HttpStatusByCode` da `413` yo'q — u Kestrel/transport darajasidagi kod,
    /// domen xatosi emas, shu sabab bu yerda aniq quriladi.
    /// </summary>
    private ObjectResult PayloadTooLarge()
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "Fayl juda katta",
            Detail = $"Ruxsat etilgan hajm — {CatalogExcelLimits.MaxFileBytes / (1024 * 1024)} MB.",
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["code"] = ProblemDetailsSetup.PayloadTooLarge;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status413PayloadTooLarge,
            ContentTypes = { "application/problem+json" },
        };
    }

    private Guid RequireAdminUserId() =>
        _currentUser.AdminUserId ?? throw new InvalidOperationException("Autentifikatsiyalangan so'rovda 'sub' claim topilmadi.");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString();
}

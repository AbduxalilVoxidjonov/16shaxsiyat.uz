using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentRoadMap.Api.Common;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Public.GetTypeCatalog;

namespace StudentRoadMap.Api.Controllers;

/// <summary>
/// Sessiyaga bog'liq bo'lmagan ommaviy (marketing) katalog — `docs/07-api-shartnoma.md`
/// 1.10-bo'lim. `PublicSessionController` dan ATAYLAB ajratilgan: u yerdagi har bir endpoint
/// maktab havolasi yoki `X-Session-Token` bilan ishlaydi, bu yerdagi kontent esa hech kimga
/// tegishli emas va ochiq sahifada (`/metodika`) ko'rsatiladi.
/// </summary>
[ApiController]
[Route("api/public")]
public sealed class PublicCatalogController : ControllerBase
{
    private readonly ISender _sender;

    public PublicCatalogController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// `GET /api/public/type-catalog` — `docs/07` 1.10-bo'lim. 16 ta shaxsiyat tipining ochiq
    /// tavsifi (`type_catalog`, `SeedData/type-catalog.json`).
    ///
    /// `[AllowAnonymous]` AYNAN ko'rsatilgan: sahifa qidiruv tizimlariga va havola orqali
    /// kirgan har qanday mehmonga ochiq bo'lishi kerak, sessiya tokeni talab qilinmaydi.
    /// Tezlik cheklovi — mavjud `PublicSchoolInfo` siyosati (IP bo'yicha 60/daqiqa, `docs/07`
    /// 4-bo'lim): ikkalasi ham autentifikatsiyasiz, keshlanadigan, o'qish uchungina bo'lgan
    /// ommaviy so'rov; shu sababdan yangi siyosat kiritilmadi.
    /// </summary>
    [HttpGet("type-catalog")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitSetup.PublicSchoolInfo)]
    [ProducesResponseType(typeof(GetTypeCatalogResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public async Task<ActionResult<GetTypeCatalogResult>> GetTypeCatalog(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTypeCatalogQuery(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error);
    }
}

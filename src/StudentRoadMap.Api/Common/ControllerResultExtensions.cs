using Microsoft.AspNetCore.Mvc;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Api.Common;

/// <summary>
/// Handler'lardan qaytgan `Result`/`Result&lt;T&gt;` muvaffaqiyatsizligini `ProblemDetails`
/// javobiga aylantiradi (`docs/06-arxitektura.md` 6-bo'lim). Istisno emas — bu handler
/// ICHIDAGI biznes qoidalari (dublikat, limit, mavjud emas va h.k.) uchun, `docs/06`/`CLAUDE.md`
/// "`Result&lt;T&gt;` — istisno biznes oqimi uchun ishlatilmaydi" qoidasiga mos.
/// </summary>
internal static class ControllerResultExtensions
{
    public static ObjectResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = ProblemCodes.HttpStatusByCode.GetValueOrDefault(error.Code, ProblemCodes.DefaultDomainErrorStatus);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Message,
            Type = $"https://studentroadmap/errors/{error.Code.ToLowerInvariant().Replace('_', '-')}",
            Instance = controller.HttpContext.Request.Path,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using ApplicationValidationException = StudentRoadMap.Application.Common.Exceptions.ValidationException;

namespace StudentRoadMap.Api.Middleware;

/// <summary>
/// Ushlanmagan barcha istisnolarni `ProblemDetails` (RFC 9457) formatiga aylantiradi —
/// `code` maydoni bilan (`docs/06-arxitektura.md` 6-bo'lim). .NET 8+ `IExceptionHandler`
/// naqshi ishlatiladi (`app.UseExceptionHandler()` + `AddExceptionHandler&lt;T&gt;`) — eski
/// qo'lda yozilgan middleware o'rniga, funksional jihatdan bir xil, testlash osonroq.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IExceptionHandler
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title, errors) = Map(exception);

        if (status >= 500)
        {
            _logger.LogError(exception, "Ushlanmagan xato: {Code}", code);
        }
        else
        {
            _logger.LogWarning(exception, "So'rov xato bilan tugadi: {Code}", code);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://studentroadmap/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;

        // `WriteAsJsonAsync` `Response.ContentType`ni o'zi qayta yozadi (parametrsiz chaqirilsa
        // `application/json`ga qaytaradi) — shu sabab `application/problem+json` shu yerda,
        // yozish chaqirig'ining o'zida, aniq ko'rsatiladi (oldindan qo'yish yetarli emas).
        await httpContext.Response
            .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    private static (int Status, string Code, string Title, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception exception) =>
        exception switch
        {
            ApplicationValidationException validation => (
                400,
                ProblemCodes.ValidationError,
                "Kiritilgan ma'lumotlar noto'g'ri.",
                validation.Errors),
            DomainException domain => (
                ProblemCodes.HttpStatusByCode.GetValueOrDefault(domain.Code, ProblemCodes.DefaultDomainErrorStatus),
                domain.Code,
                domain.Message,
                null),
            _ => (500, ProblemCodes.InternalError, "Kutilmagan xatolik yuz berdi.", null),
        };
}

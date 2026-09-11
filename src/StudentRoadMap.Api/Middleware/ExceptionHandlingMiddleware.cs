using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentRoadMap.Api.Extensions;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using ApplicationValidationException = StudentRoadMap.Application.Common.Exceptions.ValidationException;
using ConcurrencyConflictException = StudentRoadMap.Application.Common.Exceptions.ConcurrencyConflictException;
using ForeignKeyViolationException = StudentRoadMap.Application.Common.Exceptions.ForeignKeyViolationException;
using UniqueConstraintViolationException = StudentRoadMap.Application.Common.Exceptions.UniqueConstraintViolationException;

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
            // QA topilmasi (`docs/13-auth-va-jwt.md`): TOTP asosiy kod poyga holati —
            // `AppDbContext.SaveChangesAsync` EF Core `DbUpdateConcurrencyException`sini shu
            // (EF'siz) istisnoga aylantiradi. Mijoz 409 bilan qaytadan urinib ko'rishi kerak.
            ConcurrencyConflictException concurrency => (
                409,
                ProblemCodes.ConcurrencyConflict,
                concurrency.Message,
                null),
            // `P14` MAXSUS DIQQAT #3: DB unique cheklov poyga holati — tushunarli `409`,
            // jimgina `500` emas (`AppDbContext.SaveChangesAsync` izohiga qarang).
            UniqueConstraintViolationException unique => (
                ProblemCodes.HttpStatusByCode.GetValueOrDefault(unique.Code, ProblemCodes.DefaultDomainErrorStatus),
                unique.Code,
                unique.Message,
                null),
            // P52 (2026-09-11 QA topilmasi): tashqi kalit (FK) cheklovi buzilishi —
            // `UniqueConstraintViolationException` bilan bir xil naqsh, jimgina `500` emas.
            // Xabar `AppDbContext.SaveChangesAsync`dan keladi va DOIM o'zgarmas umumiy matn
            // (DB jadval/cheklov nomi hech qachon chiqmaydi, P31).
            ForeignKeyViolationException foreignKey => (
                ProblemCodes.HttpStatusByCode.GetValueOrDefault(foreignKey.Code, ProblemCodes.DefaultDomainErrorStatus),
                foreignKey.Code,
                foreignKey.Message,
                null),
            DomainException domain => (
                ProblemCodes.HttpStatusByCode.GetValueOrDefault(domain.Code, ProblemCodes.DefaultDomainErrorStatus),
                domain.Code,
                domain.Message,
                null),
            // Kestrel/model-binding transport xatosi: so'rov tanasi juda katta (413), tana
            // to'satdan uzilgan yoki so'rov qatori buzuq (400). ILGARI bularning HAMMASI
            // pastdagi `_ =>` shoxiga tushib `500 INTERNAL_ERROR` bo'lardi — ya'ni mijozning
            // xatosi server nosozligi sifatida ko'rsatilardi (P31 topilmasi). Istisno XABARI
            // ataylab ishlatilmaydi (u ichki chegara qiymatlarini oshkor qiladi, masalan
            // "The max request body size is 30000000 bytes") — o'rniga o'zbekcha umumiy matn.
            BadHttpRequestException badRequest => (
                badRequest.StatusCode,
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? ProblemDetailsSetup.PayloadTooLarge
                    : ProblemCodes.ValidationError,
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "So'rov hajmi ruxsat etilgan chegaradan katta."
                    : "So'rov noto'g'ri shakllantirilgan.",
                null),
            _ => (500, ProblemCodes.InternalError, "Kutilmagan xatolik yuz berdi.", null),
        };
}

using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Api.Middleware;
using StudentRoadMap.Application.Common.Exceptions;

namespace StudentRoadMap.Api.IntegrationTests.Middleware;

/// <summary>
/// `ExceptionHandlingMiddleware` — to'g'ridan-to'g'ri birlik sinovi (HTTP so'rovsiz/DB'siz):
/// P52 (2026-09-11 QA topilmasi) `ForeignKeyViolationException` → `409 REFERENCED_RECORD_EXISTS`
/// tarjimasi. `AppDbContext.SaveChangesAsync`ning o'zi haqiqiy Postgres/SQLite talab qiladi
/// (`AppDbContext.IsForeignKeyViolation`) — bu yerda FAQAT middleware xaritalash qoidasi
/// sinaladi, `UniqueConstraintViolationException` bilan bir xil, allaqachon ishlaydigan naqsh.
/// </summary>
public sealed class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, JsonElement Problem)> HandleAsync(Exception exception)
    {
        var middleware = new ExceptionHandlingMiddleware(NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
        };

        var handled = await middleware.TryHandleAsync(context, exception, CancellationToken.None);
        handled.Should().BeTrue();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, JsonDocument.Parse(json).RootElement.Clone());
    }

    [Fact]
    public async Task ForeignKeyViolationException_409VaKodBilanQaytadi()
    {
        var exception = new ForeignKeyViolationException(
            "REFERENCED_RECORD_EXISTS",
            "Bu amalni bajarib bo'lmadi: bu yozuvga boshqa ma'lumotlar bog'liq.");

        var (status, problem) = await HandleAsync(exception);

        status.Should().Be(409);
        problem.GetProperty("status").GetInt32().Should().Be(409);
        problem.GetProperty("code").GetString().Should().Be("REFERENCED_RECORD_EXISTS");
        problem.GetProperty("type").GetString().Should().Be("https://studentroadmap/errors/referenced-record-exists");
    }

    /// <summary>
    /// P31 qoidasi: DB darajasidagi tafsilot (jadval/cheklov nomi, kutubxona istisno turi)
    /// javobga HECH QACHON chiqmasin — `CatalogExcelWorkbookTests.Oqish_XatoXabarlariIchkiTafsilotSizdirmaydi`
    /// bilan bir xil naqsh.
    /// </summary>
    [Fact]
    public async Task ForeignKeyViolationException_XabarDbTafsilotiniOshkorQilmaydi()
    {
        var exception = new ForeignKeyViolationException(
            "REFERENCED_RECORD_EXISTS",
            "Bu amalni bajarib bo'lmadi: bu yozuvga boshqa ma'lumotlar bog'liq.");

        var (_, problem) = await HandleAsync(exception);

        string[] leaks = ["fk_", "constraint", "23503", "Postgres", "Npgsql", "DbUpdateException", "SqlState", "table"];
        var title = problem.GetProperty("title").GetString();

        title.Should().NotBeNullOrWhiteSpace();
        foreach (var leak in leaks)
        {
            title.Should().NotContain(leak);
        }
    }

    /// <summary>`UniqueConstraintViolationException` bilan bir xil naqsh ekanini nazorat qiladi — `ProblemCodes.HttpStatusByCode`ga qo'shilmagan kod bo'lsa ham (masalan kelajakda) `409` standart qiymatga tushadi.</summary>
    [Fact]
    public async Task ForeignKeyViolationException_RoyxatdaYoqKodUchunHamStandart409Beradi()
    {
        var exception = new ForeignKeyViolationException("NOMALUM_KOD", "Umumiy xato matni.");

        var (status, _) = await HandleAsync(exception);

        status.Should().Be(409);
    }
}

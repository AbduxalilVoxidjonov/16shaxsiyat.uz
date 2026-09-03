using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Security;

/// <summary>
/// P31: model-binding/JSON bosqichidagi xatolar loyihaning YAGONA `ProblemDetails` shakliga
/// (`docs/06-arxitektura.md` 6-bo'lim, `CLAUDE.md` 11-qoida) keltirilganini qulflaydi.
///
/// Topilma: `[ApiController]` standart holatda `ValidationProblemDetails` qaytarardi — unda
/// `code` maydoni YO'Q edi, ya'ni buzuq JSON yuborgan klient xatoni ajrata olmasdi va shakl
/// `ValidationBehavior` chiqaradigan javobdan farq qilardi. `ModelStateProblemDetailsSetup`
/// shu bo'shliqni yopadi.
///
/// **Alohida `IClassFixture`**: bu klass `POST /api/public/sessions` ga bir necha so'rov
/// yuboradi, u esa `RateLimitSetup.PublicStartSession` (10/soat) ostida — boshqa test
/// klasslarining kvotasiga aralashmasligi uchun o'z host nusxasi ishlatiladi
/// (`PublicStartSessionValidationBoundaryTests`dagi bilan bir xil sabab).
/// </summary>
public sealed class ProblemDetailsContractTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public ProblemDetailsContractTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static StringContent Json(string raw) =>
        new(raw, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

    /// <summary>Har uchala holat uchun majburiy shakl — `docs/06` 6-bo'limidagi misol bilan bir xil.</summary>
    private static void AssertContractShape(HttpResponseMessage response, JsonElement problem)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("status").GetInt32().Should().Be(400);
        problem.GetProperty("type").GetString().Should().Be("https://studentroadmap/errors/validation-error");
        problem.GetProperty("title").GetString().Should().Be("Kiritilgan ma'lumotlar noto'g'ri.");
        problem.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        problem.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
        problem.GetProperty("errors").EnumerateObject().Should().NotBeEmpty();
    }

    /// <summary>Buzuq JSON (`{"name":`) — ilgari `code`siz `ValidationProblemDetails` qaytardi.</summary>
    [Fact]
    public async Task BuzuqJsonTanasi_CodeBilanProblemDetailsQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/public/sessions", UriKind.Relative), Json("{\"name\":"));
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertContractShape(response, problem);
        problem.GetProperty("errors").TryGetProperty("body", out var bodyErrors).Should().BeTrue(
            "butun tanaga tegishli xato `$` emas, `body` kaliti ostida bo'lishi kerak");
        bodyErrors[0].GetString().Should().Be("So'rov tanasi (JSON) noto'g'ri formatda.");
    }

    /// <summary>Buzuq JSON javobida System.Text.Json'ning ichki tafsiloti (yo'l, satr/bayt) chiqmasligi.</summary>
    [Fact]
    public async Task BuzuqJsonTanasi_IchkiParserTafsilotiniOshkorQilmaydi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/public/sessions", UriKind.Relative), Json("{\"name\":"));
        var body = await response.Content.ReadAsStringAsync();

        body.Should().NotContain("LineNumber");
        body.Should().NotContain("BytePositionInLine");
        body.Should().NotContain("Path: $");
        body.Should().NotContain("StudentRoadMap.Api.Contracts");
    }

    /// <summary>Noto'g'ri tur (`"grade": "abc"`) — xato AYNAN o'sha maydon kaliti ostida.</summary>
    [Fact]
    public async Task NotogriTur_MaydonKalitiBilanCodeQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(
            new Uri("/api/public/sessions", UriKind.Relative),
            Json("{\"slug\":\"maktab\",\"accessToken\":\"t\",\"fullName\":\"Ali Valiyev\",\"grade\":\"abc\"}"));
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertContractShape(response, problem);
        problem.GetProperty("errors").TryGetProperty("grade", out var gradeErrors).Should().BeTrue();
        gradeErrors[0].GetString().Should().Be("Qiymat noto'g'ri formatda.");
    }

    /// <summary>Yo'q (majburiy) maydon — bir xil shakl, camelCase kalit.</summary>
    [Fact]
    public async Task YoqMaydon_CodeVaCamelCaseKalitBilanQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/public/sessions", UriKind.Relative), Json("{}"));
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        AssertContractShape(response, problem);

        var keys = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();
        keys.Should().Contain("slug");
        keys.Should().AllSatisfy(key => key.Should().NotStartWith("$"));
        keys.Should().AllSatisfy(key => char.IsUpper(key[0]).Should().BeFalse("kalitlar camelCase bo'lishi kerak"));
    }

    /// <summary>Uchala holat ham AYNAN bir xil maydon to'plamini qaytaradi (yagona shakl).</summary>
    [Fact]
    public async Task UchalaHolat_BirXilMaydonToplaminiQaytaradi()
    {
        using var client = _factory.CreateClient();

        var bodies = new[] { "{\"name\":", "{\"grade\":\"abc\"}", "{}" };
        var shapes = new List<string[]>();

        foreach (var body in bodies)
        {
            var response = await client.PostAsync(new Uri("/api/public/sessions", UriKind.Relative), Json(body));
            var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            AssertContractShape(response, problem);
            shapes.Add([.. problem.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)]);
        }

        shapes[1].Should().Equal(shapes[0]);
        shapes[2].Should().Equal(shapes[0]);
    }

    /// <summary>
    /// Marshrut topilmagan javob ham `code` bilan (`ProblemDetailsSetup` zaxira xaritasi) —
    /// ilgari `UseStatusCodePages` `code`siz `ProblemDetails` yozardi.
    /// </summary>
    [Fact]
    public async Task NomalumMarshrut_NotFoundCodeBilanQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/mavjud-emas", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    /// <summary>Marshrut bor, lekin metod noto'g'ri — `405 METHOD_NOT_ALLOWED`.</summary>
    [Fact]
    public async Task NotogriMetod_MethodNotAllowedCodeBilanQaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/sessions", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("code").GetString().Should().Be("METHOD_NOT_ALLOWED");
    }
}

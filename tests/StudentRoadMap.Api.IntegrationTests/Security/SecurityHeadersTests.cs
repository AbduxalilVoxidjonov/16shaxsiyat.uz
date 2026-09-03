using System.Net;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Security;

/// <summary>
/// P31: `docs/08-auth-va-xavfsizlik.md` 7-bo'limidagi xavfsizlik sarlavhalari API'ning HAR
/// javobida bo'lishini qulflaydi (`SecurityHeadersMiddleware`).
///
/// HSTS bu yerda sinalmaydi: `app.UseHsts()` faqat Production'da yoqiladi, integratsiya
/// host'i esa Development'da ishlaydi. Frontend (nginx) sarlavhalari `docker/web-nginx.conf`
/// da va jonli tekshiriladi.
/// </summary>
public sealed class SecurityHeadersTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public SecurityHeadersTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static void AssertBaseHeaders(HttpResponseMessage response)
    {
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");
        response.Headers.GetValues("Permissions-Policy").Should().ContainSingle().Which.Should().Be("geolocation=(), microphone=(), camera=()");
    }

    [Fact]
    public async Task MuvaffaqiyatliJavob_XavfsizlikSarlavhalariniOzIchigaOladi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertBaseHeaders(response);
        response.Headers.GetValues("Content-Security-Policy").Should().ContainSingle()
            .Which.Should().Be("default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
    }

    /// <summary>
    /// Xato javobida ham sarlavhalar saqlanadi. Bu ALOHIDA test: `UseExceptionHandler`
    /// javobni (status + BARCHA sarlavhalar) tozalab qayta ishga tushiradi — shu sabab
    /// `SecurityHeadersMiddleware` sarlavhalarni `Response.OnStarting` orqali qo'yadi.
    /// </summary>
    [Fact]
    public async Task XatoJavobi_XavfsizlikSarlavhalariniYoqotmaydi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/public/mavjud-emas", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertBaseHeaders(response);
    }

    /// <summary>
    /// CSP hech qanday siyosatda `unsafe-eval` ni o'z ichiga olmaydi (`prompts/31` cheklovi).
    /// </summary>
    [Fact]
    public async Task Csp_UnsafeEvalniOzIchigaOlmaydi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        response.Headers.GetValues("Content-Security-Policy").Single().Should().NotContain("unsafe-eval");
    }

    /// <summary>
    /// Swagger UI (faqat Development) inline skript/stildan foydalanadi — unga CSP QO'YILMAYDI,
    /// aks holda hujjat sahifasi oq ekranga aylanardi. Qolgan sarlavhalar esa saqlanadi.
    /// </summary>
    [Fact]
    public async Task SwaggerSahifasi_CspSizLekinQolganSarlavhalarBilan()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/swagger/index.html", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertBaseHeaders(response);
        response.Headers.Contains("Content-Security-Policy").Should().BeFalse();
    }
}

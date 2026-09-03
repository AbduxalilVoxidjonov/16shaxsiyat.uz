using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Api.IntegrationTests.Testing;

namespace StudentRoadMap.Api.IntegrationTests.Security;

/// <summary>
/// P31 topilmasi: `429` javobida `Retry-After` sarlavhasi yo'q edi — mijoz qancha kutishni
/// bilmasdi va darhol qayta urinardi (bu limitni yanada chuqurlashtiradi). `RateLimitSetup`
/// ning YAGONA `OnRejected` handler'i barcha siyosatlar uchun sarlavhani va tanadagi
/// `retryAfterSeconds` maydonini qo'yadi; quyida ikkita TURLI oyna uzunligi (1 soat va
/// 5 daqiqa) bilan tekshiriladi — ya'ni qiymat siyosatga mos, qotirilgan emas.
///
/// **Alohida `IClassFixture`**: bu klass limiterlarni ataylab TUGATADI, shu sabab o'z host
/// nusxasi bilan ishlaydi (`AdminApiRateLimitTests`dagi bilan bir xil sabab).
/// Chegara qiymatlari (`10/soat`, `10/5 daqiqa`) `docs/07-api-shartnoma.md` 4-bo'limidan —
/// bu testda o'zgartirilmaydi.
/// </summary>
public sealed class RateLimitRetryAfterTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public RateLimitRetryAfterTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task AssertRetryAfterAsync(HttpResponseMessage response, int expectedSeconds)
    {
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        response.Headers.TryGetValues("Retry-After", out var values).Should().BeTrue("429 javobi `Retry-After` bermay qolmasligi kerak");
        var headerSeconds = int.Parse(values!.Single(), System.Globalization.CultureInfo.InvariantCulture);
        headerSeconds.Should().BeInRange(1, expectedSeconds);

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("code").GetString().Should().Be("RATE_LIMITED");
        problem.GetProperty("retryAfterSeconds").GetInt32().Should().Be(headerSeconds);
        problem.GetProperty("detail").GetString().Should().Contain(headerSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>`POST /api/public/sessions` — 10/soat (`RateLimitSetup.PublicStartSession`).</summary>
    [Fact]
    public async Task OmmaviySessiyaLimiti_RetryAfterBilan429Qaytaradi()
    {
        using var client = _factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await client.PostAsJsonAsync("/api/public/sessions", new { }, TestJson.Options);
        }

        await AssertRetryAfterAsync(last!, expectedSeconds: 3600);
    }

    /// <summary>`POST /api/auth/login` — 10/5 daqiqa (`RateLimitSetup.AdminLogin`), boshqa oyna uzunligi.</summary>
    [Fact]
    public async Task AdminLoginLimiti_QisqaroqRetryAfterQaytaradi()
    {
        using var client = _factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "mavjud-emas", password = "notogri-parol" },
                TestJson.Options);
        }

        await AssertRetryAfterAsync(last!, expectedSeconds: 300);
    }
}

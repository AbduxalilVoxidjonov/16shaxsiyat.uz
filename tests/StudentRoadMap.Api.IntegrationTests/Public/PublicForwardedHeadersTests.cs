using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `ForwardedHeadersSetup` — `docs/13` 4-bo'lim (Caddy reverse-proksi), `CLAUDE.md` 8-9-band
/// (spoofing himoyasi). Har bir test class'i O'ZINING `PublicApiTestFactory` nusxasiga ega
/// (xUnit `IClassFixture` — bitta nusxa faqat o'sha class ichidagi testlar orasida bo'lishiladi),
/// shu sabab `PublicStartSession` IP rate-limiti (10/soat) testlar orasida chalkashmaydi.
/// </summary>
public static class PublicForwardedHeadersTests
{
    internal static StartSessionCommand ValidCommand(string slug, string token, string fullName, DateOnly birthDate) =>
        new(
            Slug: slug,
            AccessToken: token,
            AccessCode: null,
            FullName: fullName,
            BirthDate: birthDate,
            Gender: Gender.Male,
            Grade: 9,
            ClassLetter: "B",
            Phone: "+998901234567",
            ParentPhone: null,
            Email: null,
            ConsentAccepted: true,
            LanguageCode: "uz");
}

/// <summary>`App:KnownProxies` konfiguratsiyada YO'Q (standart) — `X-Forwarded-For` audit/IP-xesh uchun e'tiborsiz qoldirilishi.</summary>
public sealed class PublicForwardedHeadersEmptyProxiesTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicForwardedHeadersEmptyProxiesTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task XForwardedForSpoofQilinsa_HaqiqiyUlanishManzilidanIpXeshiOlinadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("fwd-empty1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-fwd-empty1", token);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "FWD1", 1, questionCount: 1);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.99");

        var command = PublicForwardedHeadersTests.ValidCommand(school.Slug.Value, token, "Egamova Nilufar Rustamovna", new DateOnly(2010, 3, 3));
        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.SingleAsync(a => a.Id == body!.AssessmentId);

        // `TestServer`da haqiqiy ulanish manzili `null` (real IP yo'q) — `ForwardedHeadersSetup`
        // header'ni umuman qabul qilmagani uchun `IpHasher.Hash(null)` ham `null` qaytaradi.
        // Agar spoofing header qabul qilingan bo'lsa, bu yerda "203.0.113.99" xeshi chiqar edi.
        var ipHasher = verifyScope.ServiceProvider.GetRequiredService<IIpHasher>();
        assessment.IpHash.Should().BeNull();
        assessment.IpHash.Should().NotBe(ipHasher.Hash("203.0.113.99"));
    }
}

/// <summary>
/// `App:KnownProxies` bo'sh bo'lganda soxta (turli qiymatli) `X-Forwarded-For` header'lar
/// bilan IP-asosli tezlik cheklovini (`RateLimitSetup.PublicStartSession`, 10/soat) chetlab
/// o'tib bo'lmasligi — alohida fixture (o'zining limiti) uchun alohida class'da.
/// </summary>
public sealed class PublicForwardedHeadersEmptyProxiesRateLimitTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicForwardedHeadersEmptyProxiesRateLimitTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task XForwardedForHarXilQiymatBilanSpoofQilinsa_RateLimitHaqiqiyManzilBoyichaIshlaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("fwd-empty2");
        // Kunlik ro'yxatdan o'tish limiti baland — bu testda faqat IP RateLimiter (10/soat) sinaladi.
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-fwd-empty2", token, dailyRegistrationLimit: 500);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "FWD2", 1, questionCount: 1);

        using var client = _factory.CreateClient();

        // `RateLimitSetup.PublicStartSession` — 10/soat. Har so'rov BOSHQA soxta
        // `X-Forwarded-For` bilan yuboriladi — agar header'ga ishonilsa, har biri alohida
        // "IP" hisoblanib, limitga hech qachon urilmasdi (bitta bot butun maktabni bloklashi
        // yoki limit umuman ta'sir qilmasligi mumkin bo'lgan xavf shu yerdan kelib chiqadi).
        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/public/sessions")
            {
                Content = JsonContent.Create(
                    PublicForwardedHeadersTests.ValidCommand(school.Slug.Value, token, $"Spoof Testov Ismoilovich{i:00}", new DateOnly(2010, 1, 1).AddDays(i)),
                    options: TestJson.Options),
            };
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{i}");

            last = await client.SendAsync(request);
        }

        last.Should().NotBeNull();
        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "spoofing bilan haqiqiy ulanish manziliga asoslangan IP rate-limit chetlab o'tilmasligi kerak");
    }
}

/// <summary>`App:KnownProxies`da bitta ishonchli proksi ko'rsatilgan — `X-Forwarded-For`dagi mijoz IP'si ishlatiladi.</summary>
public sealed class PublicForwardedHeadersTrustedProxyTests : IClassFixture<TrustedProxyApiTestFactory>
{
    private readonly TrustedProxyApiTestFactory _factory;

    public PublicForwardedHeadersTrustedProxyTests(TrustedProxyApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task XForwardedForYuborilsa_HeaderdagiMijozIpsiIshlatiladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("fwd-trusted1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-fwd-trusted1", token);
        await TestDataFactory.CreatePublishedTestAsync(db, now, "FWD3", 1, questionCount: 1);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "198.51.100.42");

        var command = PublicForwardedHeadersTests.ValidCommand(school.Slug.Value, token, "Nabiyev Jasur Alisherovich", new DateOnly(2011, 2, 2));
        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StartSessionResult>(TestJson.Options);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assessment = await verifyDb.Assessments.SingleAsync(a => a.Id == body!.AssessmentId);

        var ipHasher = verifyScope.ServiceProvider.GetRequiredService<IIpHasher>();
        assessment.IpHash.Should().Be(ipHasher.Hash("198.51.100.42"));
    }
}

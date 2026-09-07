using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Public.ResolveSchoolCode;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `POST /api/public/schools/resolve-code` — `docs/07` 1.1a, `docs/08` 3a. Kod →
/// `{ slug, accessToken }`; barcha rad holatlari BITTA `404 SCHOOL_CODE_INVALID`
/// (enumeration'ga yo'l qo'ymaslik). Alohida `IClassFixture` — rate limit (10/5 daqiqa/IP)
/// bilan bo'lishilgan host'da boshqa testlar kvotani yeb qo'ymasligi uchun; bu klassda
/// jami 10 tadan kam so'rov yuboriladi.
/// </summary>
public sealed class PublicResolveSchoolCodeEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicResolveSchoolCodeEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ToGriKod_DefisVaKichikHarfBilanHam_200SlugVaTokenQaytaradi()
    {
        var now = DateTimeOffset.UtcNow;
        var accessToken = TestDataFactory.NewAccessToken("resolve-ok");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.CreateSchoolAsync(db, now, "resolve-ok-maktab", accessToken, entryCode: "RSXV2345");
        }

        using var client = _factory.CreateClient();

        // Foydalanuvchi kiritgan XOM shakl: kichik harf + defis + bo'shliq.
        var response = await client.PostAsJsonAsync("/api/public/schools/resolve-code", new { code = " rsxv-2345 " }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<ResolveSchoolCodeResult>(TestJson.Options))!;
        body.Slug.Should().Be("resolve-ok-maktab");
        body.AccessToken.Should().Be(accessToken, "kod bilan kirgan o'quvchi havola bilan kirgan bilan bir xil huquqga ega");

    }

    [Fact]
    public async Task NotoGriNofaolOchirilganKodlar_BirXil404_VaAuditYoziladi()
    {
        // Muvaffaqiyatli `resolve` audit qilinmaydi (havola ochilishi kabi) — shu sabab bu
        // testda faqat RAD holatlari sanaladi: `failedBefore` → `+ attempts.Length`.
        var now = DateTimeOffset.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.CreateSchoolAsync(db, now, "resolve-inactive-maktab", TestDataFactory.NewAccessToken("resolve-inactive"), isActive: false, entryCode: "NFAK2345");
            var deleted = await TestDataFactory.CreateSchoolAsync(db, now, "resolve-deleted-maktab", TestDataFactory.NewAccessToken("resolve-deleted"), entryCode: "UCHR2345");
            deleted.MarkDeleted(now);
            await db.SaveChangesAsync();
            // Ommaviy makon — `EntryCode == null`; unga hech qanday kod olib bormaydi.
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
        }

        using var client = _factory.CreateClient();

        int failedBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            failedBefore = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .AuditLogs.CountAsync(a => a.Action == SchoolEntryCodeAuditActions.ResolveFailed);
        }

        var attempts = new[]
        {
            "YUQQ2345", // mavjud emas
            "NFAK2345", // nofaol maktab — `410` EMAS, xuddi "yo'q" kabi
            "UCHR2345", // o'chirilgan maktab
            "bad",      // format noto'g'ri — ham bir xil javob
            "",         // bo'sh
        };

        foreach (var attempt in attempts)
        {
            var response = await client.PostAsJsonAsync("/api/public/schools/resolve-code", new { code = attempt }, TestJson.Options);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"'{attempt}' uchun bitta umumiy 404 kutiladi");
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            problem.GetProperty("code").GetString().Should().Be("SCHOOL_CODE_INVALID");
            problem.GetProperty("title").GetString().Should().Be("Kod topilmadi. Maktabingizdan tekshiring.");
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var failedLogs = await verifyDb.AuditLogs
            .Where(a => a.Action == SchoolEntryCodeAuditActions.ResolveFailed)
            .ToListAsync();

        failedLogs.Count.Should().Be(failedBefore + attempts.Length, "har rad etilgan urinish audit'ga tushadi");
        failedLogs.Should().OnlyContain(a => a.AdminUserId == null && a.EntityId == null, "maktab aniqlanmagan — `EntityId` yo'q; ommaviy oqim — admin yo'q");
        failedLogs.Should().OnlyContain(a => a.AfterJson == null && a.BeforeJson == null, "kiritilgan kod QIYMATI saqlanmaydi");
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Admin.Schools.RegenerateEntryCode;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Application.Public.ResolveSchoolCode;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// Maktab kodi admin tomonida — `docs/07` 3.1: yaratish javobida `entryCode` bor, detalda
/// ko'rinadi, `regenerate-entry-code` eskisini bekor qiladi (ommaviy `resolve-code` bilan
/// uchma-uch tekshiriladi). Alohida `IClassFixture`: `resolve-code` kvotasi (10/5 daqiqa)
/// shu klass ichida 4 tadan oshmaydi.
/// </summary>
public sealed class AdminSchoolEntryCodeEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private const string FormattedEntryCodePattern = "^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{4}-[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{4}$";

    private readonly PublicApiTestFactory _factory;

    public AdminSchoolEntryCodeEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, username);
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username, password = AdminTestDataFactory.DefaultPassword },
            TestJson.Options);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    [Fact]
    public async Task Create_JavobdaFormatlanganEntryCodeBor_VaDetaldaHamKorinadi()
    {
        using var client = await AuthenticatedClientAsync("entry-code-create-admin");

        var response = await client.PostAsJsonAsync("/api/admin/schools", new
        {
            name = "Kodli maktab",
            region = "Farg'ona",
            district = "Qo'qon",
            schoolNumber = (string?)null,
            contactPerson = (string?)null,
            contactPhone = (string?)null,
            accessCode = (string?)null,
            dailyRegistrationLimit = (int?)null,
            notes = (string?)null,
        }, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        created.EntryCode.Should().NotBeNull("kod yaratishda AVTOMATIK paydo bo'ladi — admin kiritmaydi");
        created.EntryCode.Should().MatchRegex(FormattedEntryCodePattern, "ko'rsatish shakli `XXXX-XXXX`, alifboda 0/O/1/I/L yo'q");
        created.AccessCode.Should().BeNull("`accessCode` (sinf kodi) — boshqa maydon, tegilmaydi");

        var detail = (await client.GetFromJsonAsync<AdminSchoolDetailDto>($"/api/admin/schools/{created.Id}", TestJson.Options))!;
        detail.EntryCode.Should().Be(created.EntryCode);

        // Bazada defissiz saqlanadi.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Schools.Where(s => s.Id == created.Id).Select(s => s.EntryCode).SingleAsync();
        stored.Should().Be(created.EntryCode!.Replace("-", string.Empty));
    }

    [Fact]
    public async Task RegenerateEntryCode_EskiKod404_YangisiIshlaydi_AuditYoziladi()
    {
        var now = DateTimeOffset.UtcNow;
        Guid schoolId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var school = await TestDataFactory.CreateSchoolAsync(db, now, "regen-code-maktab", TestDataFactory.NewAccessToken("regen-code"), entryCode: "RGNR2345");
            schoolId = school.Id;
        }

        using var admin = await AuthenticatedClientAsync("entry-code-regen-admin");
        using var publicClient = _factory.CreateClient();

        // Eski kod hozircha ishlaydi.
        (await publicClient.PostAsJsonAsync("/api/public/schools/resolve-code", new { code = "RGNR-2345" }, TestJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await admin.PostAsync($"/api/admin/schools/{schoolId}/regenerate-entry-code", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<RegenerateSchoolEntryCodeResult>(TestJson.Options))!;
        body.EntryCode.Should().MatchRegex(FormattedEntryCodePattern);
        body.EntryCode.Should().NotBe("RGNR-2345");

        // Eski kod DARHOL yaroqsiz; yangisi ishlaydi va o'sha maktabga olib boradi.
        (await publicClient.PostAsJsonAsync("/api/public/schools/resolve-code", new { code = "RGNR2345" }, TestJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        var resolved = await publicClient.PostAsJsonAsync("/api/public/schools/resolve-code", new { code = body.EntryCode }, TestJson.Options);
        resolved.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resolved.Content.ReadFromJsonAsync<ResolveSchoolCodeResult>(TestJson.Options))!.Slug.Should().Be("regen-code-maktab");

        // Detal yangi kodni ko'rsatadi; havola (`publicUrl`) O'ZGARMAGAN — kod va token mustaqil.
        var detail = (await admin.GetFromJsonAsync<AdminSchoolDetailDto>($"/api/admin/schools/{schoolId}", TestJson.Options))!;
        detail.EntryCode.Should().Be(body.EntryCode);
        detail.PublicUrl.Should().Contain("k=");

        using var verifyScope = _factory.Services.CreateScope();
        var db2 = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db2.AuditLogs.SingleAsync(a => a.Action == SchoolEntryCodeAuditActions.Regenerated && a.EntityId == schoolId);
        audit.AdminUserId.Should().NotBeNull();
        audit.AfterJson.Should().NotContain(body.EntryCode.Replace("-", string.Empty), "kod QIYMATI audit'da saqlanmaydi (havola tokeni bilan bir xil qoida)");
    }

    [Fact]
    public async Task RegenerateEntryCode_NomalumYokiOmmaviyMakon_404()
    {
        var now = DateTimeOffset.UtcNow;
        Guid publicSpaceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            publicSpaceId = (await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now)).Id;
        }

        using var admin = await AuthenticatedClientAsync("entry-code-404-admin");

        var unknown = await admin.PostAsync($"/api/admin/schools/{Guid.NewGuid()}/regenerate-entry-code", content: null);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Ommaviy makon admin maktab endpointlariga KIRMAYDI (`SchoolsOnly`) — unda kod yo'q.
        var publicSpace = await admin.PostAsync($"/api/admin/schools/{publicSpaceId}/regenerate-entry-code", content: null);
        publicSpace.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await publicSpace.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }
}

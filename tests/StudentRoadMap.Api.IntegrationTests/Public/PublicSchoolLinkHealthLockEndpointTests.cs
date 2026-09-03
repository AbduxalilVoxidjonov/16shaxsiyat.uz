using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// **MEZON QULFI (HTTP darajasi)** — 2026-09-03 jonli hodisasi.
///
/// Admin paneldagi "havola ishlaydi/ishlamaydi" belgisi (`linkHealth`) va o'quvchi ko'radigan
/// ommaviy javob bir xil haqiqatni aytishi SHART. `SchoolLinkHealthCriterionTests` buni handler
/// darajasida (barcha kombinatsiyalar bo'yicha) qulflaydi; bu yerda ayni narsa HAQIQIY HTTP
/// javoblari ustidan, XOM JSON bilan tekshiriladi.
///
/// **Alohida `IClassFixture`** — SHART: bu test ikkinchi bosqichda standart `Public` dasturni
/// NASHR QILADI (butun in-memory baza uchun), shu sabab u "dastursiz maktab" testlari bilan
/// bitta bazani baham ko'ra olmaydi (`PublicSchoolInfoNoProgramEndpointTests`).
/// </summary>
public sealed class PublicSchoolLinkHealthLockEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicSchoolLinkHealthLockEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    [Fact]
    public async Task PanelBelgisi_OmmaviyJavobBilanBirXilHaqiqatniAytadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken("noprogram-lock");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "noprogram-maktab-lock", token);

        using var publicClient = _factory.CreateClient();
        using var adminClient = await AuthenticatedClientAsync("noprogram-lock-admin");

        // ————— 1-bosqich: dastur YO'Q —————
        var beforePublic = await publicClient.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));
        var beforeHealth = await ReadLinkHealthAsync(adminClient, school.Id);

        beforePublic.StatusCode.Should().Be(HttpStatusCode.Conflict);
        beforeHealth.GetProperty("availableProgramCount").GetInt32().Should().Be(0);
        beforeHealth.GetProperty("status").GetString().Should().NotBe("Ok");

        // ————— 2-bosqich: nashr qilingan test standart `Public` dasturga biriktiriladi —————
        await TestDataFactory.CreatePublishedTestAsync(db, now, "LOCK_TEST", displayOrder: 1);

        var afterPublic = await publicClient.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={token}", UriKind.Relative));
        var afterHealth = await ReadLinkHealthAsync(adminClient, school.Id);

        afterPublic.StatusCode.Should().Be(HttpStatusCode.OK);
        afterHealth.GetProperty("availableProgramCount").GetInt32().Should().BeGreaterThan(0);
        afterHealth.GetProperty("status").GetString().Should().Be(
            "Ok",
            "panel belgisi ommaviy javob bilan bir xil mezondan hisoblanadi — ular ajralsa panel yolg'on aytadi");
    }

    private async Task<JsonElement> ReadLinkHealthAsync(HttpClient adminClient, Guid schoolId)
    {
        var response = await adminClient.GetAsync(new Uri($"/api/admin/schools/{schoolId}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await ReadJsonAsync(response)).GetProperty("linkHealth").Clone();
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
}

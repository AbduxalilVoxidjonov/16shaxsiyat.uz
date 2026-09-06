using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// **Ommaviy makon admin "Maktablar" bo'limida UMUMAN ko'rinmasligi** — `AdminSchoolScope`
/// (2026-09-06, egasining e'tirozi: "unga alohida bo'lim va'da qilingan edi, u esa maktablar
/// ro'yxatida oddiy qator bo'lib turibdi").
///
/// <para>
/// Bu sinf ATAYLAB REGRESSIYA QULFI: ommaviy makon `schools` jadvalidagi qator bo'lib qolgani
/// uchun (`SchoolKind` izohi: `SchoolId` 42 faylda majburiy, nullable qilinmadi) har qanday
/// yangi admin so'rovi uni "yana" ro'yxatga qo'shib yuborishi juda oson. Filtr yo'qolsa —
/// birinchi test yiqiladi.
/// </para>
/// <para>
/// Mutatsiya endpointlari `404` qaytaradi (`403` emas): "Maktablar" bo'limi uchun bu yozuv
/// MAVJUD EMAS. `403` "bor, lekin ruxsat yo'q" degan ma'noni berardi va adminni ID bo'yicha
/// qayta urinishga undardi.
/// </para>
/// </summary>
public sealed class AdminPublicSpaceSeparationEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminPublicSpaceSeparationEndpointTests(PublicApiTestFactory factory)
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

    private async Task<(Guid PublicSpaceId, Guid SchoolId)> SeedSpaceAndSchoolAsync(string slugSeed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var space = await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, slugSeed, TestDataFactory.NewAccessToken(slugSeed));

        return (space.Id, school.Id);
    }

    [Fact]
    public async Task List_OmmaviyMakon_MaktablarRoyxatidaYoq()
    {
        var (publicSpaceId, schoolId) = await SeedSpaceAndSchoolAsync("ajratish-list-1");

        using var client = await AuthenticatedClientAsync("separation-list-admin");

        var page = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?page=1&pageSize=100", TestJson.Options);

        page.Should().NotBeNull();
        page!.Items.Should().Contain(s => s.Id == schoolId, "oddiy maktab ro'yxatda qolishi kerak");
        page.Items.Should().NotContain(
            s => s.Id == publicSpaceId,
            "ommaviy makon MAKTAB EMAS — uning o'z bo'limi bor (`/api/admin/public-space`)");
    }

    [Fact]
    public async Task List_NomiBoyichaQidiruv_OmmaviyMakonniTopmaydi()
    {
        var (publicSpaceId, _) = await SeedSpaceAndSchoolAsync("ajratish-search-1");

        using var client = await AuthenticatedClientAsync("separation-search-admin");

        // Qidiruv — filtrni chetlab o'tishning eng oson yo'li: makon nomining bir qismi
        // bo'yicha qidirilsa ham u chiqmasligi kerak.
        var found = await client.GetFromJsonAsync<PagedResult<AdminSchoolListItemDto>>(
            "/api/admin/schools?search=Ommaviy&pageSize=100", TestJson.Options);

        found.Should().NotBeNull();
        found!.Items.Should().NotContain(s => s.Id == publicSpaceId);
    }

    [Fact]
    public async Task GetById_OmmaviyMakon_404Qaytaradi()
    {
        var (publicSpaceId, _) = await SeedSpaceAndSchoolAsync("ajratish-getbyid-1");

        using var client = await AuthenticatedClientAsync("separation-getbyid-admin");

        var response = await client.GetAsync(new Uri($"/api/admin/schools/{publicSpaceId}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleActive_OmmaviyMakon_404Qaytaradi()
    {
        var (publicSpaceId, _) = await SeedSpaceAndSchoolAsync("ajratish-toggle-1");

        using var client = await AuthenticatedClientAsync("separation-toggle-admin");

        // Domen buni `SCHOOL_PUBLIC_SPACE_PROTECTED` bilan taqiqlaydi — ko'lam filtri esa
        // handler domengacha yetib bormasligini ta'minlaydi (ikki qatlamli himoya).
        var response = await client.PostAsync(
            new Uri($"/api/admin/schools/{publicSpaceId}/toggle-active", UriKind.Relative),
            content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVaRegenerateLink_OmmaviyMakon_404Qaytaradi()
    {
        var (publicSpaceId, _) = await SeedSpaceAndSchoolAsync("ajratish-delete-1");

        using var client = await AuthenticatedClientAsync("separation-delete-admin");

        var deleteResponse = await client.DeleteAsync(new Uri($"/api/admin/schools/{publicSpaceId}", UriKind.Relative));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Ommaviy oqim maxfiy havolaga tayanmaydi (kirish `/kirish` sahifasi orqali), shu
        // sabab "havolani yangilash" bu makon uchun ma'nosiz.
        var regenerateResponse = await client.PostAsync(
            new Uri($"/api/admin/schools/{publicSpaceId}/regenerate-link", UriKind.Relative),
            content: null);
        regenerateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_OmmaviyMakon_404Qaytaradi()
    {
        var (publicSpaceId, _) = await SeedSpaceAndSchoolAsync("ajratish-update-1");

        using var client = await AuthenticatedClientAsync("separation-update-admin");

        var response = await client.PutAsJsonAsync(
            $"/api/admin/schools/{publicSpaceId}",
            new
            {
                name = "Qayta nomlangan makon",
                region = "Toshkent",
                district = "Chilonzor",
                schoolNumber = (string?)null,
                contactPerson = (string?)null,
                contactPhone = (string?)null,
                accessCode = (string?)null,
                dailyRegistrationLimit = (int?)500,
                notes = (string?)null,
            },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkHealth_OmmaviyMakon_MaktabSifatidaSanalmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
        var schoolCountBefore = db.Schools.Count(s => s.IsActive && s.Kind == Domain.Schools.SchoolKind.School);

        using var client = await AuthenticatedClientAsync("separation-health-admin");

        var health = await client.GetFromJsonAsync<AdminSchoolsLinkHealthDto>(
            "/api/admin/schools/link-health", TestJson.Options);

        health.Should().NotBeNull();
        health!.ActiveSchoolCount.Should().Be(
            schoolCountBefore,
            "ommaviy makon 'faol maktablar' hisobiga qo'shilmasligi kerak — bu signal faqat maktab havolalari haqida");
        health.Schools.Should().NotContain(s => s.Name.Contains("Ommaviy", StringComparison.OrdinalIgnoreCase));
    }
}

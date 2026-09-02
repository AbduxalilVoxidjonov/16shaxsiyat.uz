using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Admin.Schools.RegenerateLink;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `SchoolsController.Update/ToggleActive/RegenerateLink/Delete` — `docs/07` 3.1-bo'lim,
/// `prompts/14` MAXSUS DIQQAT #4/#5. Alohida `IClassFixture`.
/// </summary>
public sealed class AdminSchoolsMutationEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSchoolsMutationEndpointTests(PublicApiTestFactory factory)
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
    public async Task Update_ToGriMalumot_MaydonlarniYangilaydiVaSlugOzgarmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "update-maktab-1", TestDataFactory.NewAccessToken("update-1"));

        using var client = await AuthenticatedClientAsync("schools-update-admin");

        var request = new
        {
            name = "Yangilangan nom",
            region = "Toshkent",
            district = "Chilonzor",
            schoolNumber = "12",
            contactPerson = "Aliyev Vali",
            contactPhone = "+998901112233",
            accessCode = (string?)null,
            dailyRegistrationLimit = 600,
            notes = "Izoh",
        };

        var response = await client.PutAsJsonAsync($"/api/admin/schools/{school.Id}", request, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        body.Name.Should().Be("Yangilangan nom");
        body.DailyRegistrationLimit.Should().Be(600);
        body.Slug.Should().Be(school.Slug.Value);

        // P23 talabi: `GetById` javobida ham joriy havolaning QR kodi bo'lishi kerak —
        // havolani o'zgartirmasdan (`regenerate-link` chaqirmasdan).
        var getByIdResponse = await client.GetAsync(new Uri($"/api/admin/schools/{school.Id}", UriKind.Relative));
        var getByIdBody = (await getByIdResponse.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        getByIdBody.QrCodeBase64.Should().NotBeNullOrWhiteSpace();
        Convert.FromBase64String(getByIdBody.QrCodeBase64);
    }

    [Fact]
    public async Task ToggleActive_FaolMaktabni_NofaolgaOtkazadiVaAksincha()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "toggle-maktab-1", TestDataFactory.NewAccessToken("toggle-1"));

        using var client = await AuthenticatedClientAsync("schools-toggle-admin");

        var firstToggle = await client.PostAsync(new Uri($"/api/admin/schools/{school.Id}/toggle-active", UriKind.Relative), content: null);
        firstToggle.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = (await firstToggle.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        firstBody.IsActive.Should().BeFalse();

        var secondToggle = await client.PostAsync(new Uri($"/api/admin/schools/{school.Id}/toggle-active", UriKind.Relative), content: null);
        var secondBody = (await secondToggle.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        secondBody.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegenerateLink_YangiTokenBeradiVaEskiHavolaDarholIshlamayQoladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var oldAccessToken = TestDataFactory.NewAccessToken("regen-old-1");
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "regen-maktab-1", oldAccessToken);

        using var client = await AuthenticatedClientAsync("schools-regen-admin");

        // Eski havola hozircha ishlaydi.
        var beforeResponse = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={oldAccessToken}", UriKind.Relative));
        beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var regenerateResponse = await client.PostAsync(new Uri($"/api/admin/schools/{school.Id}/regenerate-link", UriKind.Relative), content: null);
        regenerateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regenerateBody = (await regenerateResponse.Content.ReadFromJsonAsync<RegenerateSchoolLinkResult>(TestJson.Options))!;

        regenerateBody.PublicUrl.Should().Contain(school.Slug.Value);
        regenerateBody.QrCodeBase64.Should().NotBeNullOrWhiteSpace();
        Convert.FromBase64String(regenerateBody.QrCodeBase64); // yaroqli Base64/PNG bo'lishi kerak — istisno otilmasa yetarli.

        // Eski havola DARHOL ishlamay qoladi (`docs/04` 2.1 invarianti).
        var afterOldResponse = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={oldAccessToken}", UriKind.Relative));
        afterOldResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Yangi havola ishlaydi.
        var newAccessToken = new Uri(regenerateBody.PublicUrl).Query.TrimStart('?').Split('=', 2)[1];
        var afterNewResponse = await client.GetAsync(new Uri($"/api/public/schools/{school.Slug.Value}?k={newAccessToken}", UriKind.Relative));
        afterNewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_OquvchisiYoqMaktabni_204Qaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "delete-empty-maktab-1", TestDataFactory.NewAccessToken("delete-empty-1"));

        using var client = await AuthenticatedClientAsync("schools-delete-empty-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/schools/{school.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDelete = await client.GetAsync(new Uri($"/api/admin/schools/{school.Id}", UriKind.Relative));
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OquvchisiBorMaktabni_409SchoolHasStudentsQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "delete-withstudent-maktab-1", TestDataFactory.NewAccessToken("delete-withstudent-1"));

        var phone = PhoneNumber.Create("+998901234567").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Test Oquvchi Familiyasi", new DateOnly(2010, 1, 1), Gender.Male, 9, phone, now, now);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("schools-delete-withstudent-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/schools/{school.Id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>(TestJson.Options);
        problem!.Code.Should().Be(ProblemCodes.SchoolHasStudents);
    }

    private sealed record ProblemDetailsBody(string Code);
}

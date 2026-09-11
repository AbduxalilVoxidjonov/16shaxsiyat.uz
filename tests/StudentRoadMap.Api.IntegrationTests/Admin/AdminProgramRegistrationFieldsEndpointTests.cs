using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Programs;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// P52 kengaytmasi (2026-09-11, egasining qarori): `AssessmentProgram.RegistrationFields` —
/// admin API (`docs/18-tarmoqlanuvchi-sorovnoma.md` §9.5). Alohida `IClassFixture` —
/// `AdminProgramRegistrationModeEndpointTests`/`AdminAssessmentProgramsEndpointTests` bilan
/// bitta klassda `AdminLogin` rate limiteri (10/5 daqiqa/IP) bo'linib ketmasligi uchun.
/// </summary>
public sealed class AdminProgramRegistrationFieldsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminProgramRegistrationFieldsEndpointTests(PublicApiTestFactory factory)
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
    public async Task Create_RegistrationFieldsBerilmasa_StandartQiymatlarQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("programs-regfields-default-admin");

        var response = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGFIELDS-DEFAULT-1", nameUz = "Standart maydonlar dasturi", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        body!.RegistrationFields.BirthDate.Should().Be("Required");
        body.RegistrationFields.Gender.Should().Be("Required");
        body.RegistrationFields.Grade.Should().Be("Required");
        body.RegistrationFields.ClassLetter.Should().Be("Optional");
        body.RegistrationFields.Phone.Should().Be("Required");
        body.RegistrationFields.ParentPhone.Should().Be("Optional");
        body.RegistrationFields.Email.Should().Be("Optional");
    }

    [Fact]
    public async Task Create_RegistrationFieldsQismanBerilsa_QolganiStandartBoLibQoladi()
    {
        using var client = await AuthenticatedClientAsync("programs-regfields-partial-admin");

        var response = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new
            {
                code = "REGFIELDS-PARTIAL-1",
                nameUz = "Qisman moslashtirilgan dastur",
                descriptionUz = (string?)null,
                displayOrder = 1,
                visibility = "Assigned",
                registrationFields = new { phone = "Hidden", email = "Required" },
            },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        body!.RegistrationFields.Phone.Should().Be("Hidden");
        body.RegistrationFields.Email.Should().Be("Required");
        body.RegistrationFields.BirthDate.Should().Be("Required", "berilmagan maydonlar standart qiymatga tushishi kerak");
        body.RegistrationFields.Gender.Should().Be("Required", "berilmagan maydonlar standart qiymatga tushishi kerak");
    }

    [Fact]
    public async Task Update_RegistrationFields_SaqlanibQoladiVaGetByIdDaKoRinadi()
    {
        using var client = await AuthenticatedClientAsync("programs-regfields-update-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGFIELDS-UPD-1", nameUz = "Yangilanadigan dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/programs/{created!.Id}",
            new
            {
                nameUz = created.NameUz,
                descriptionUz = (string?)null,
                displayOrder = 1,
                visibility = "Assigned",
                registrationFields = new { classLetter = "Required", parentPhone = "Required" },
            },
            TestJson.Options);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        updated!.RegistrationFields.ClassLetter.Should().Be("Required");
        updated.RegistrationFields.ParentPhone.Should().Be("Required");

        var fetched = await client.GetFromJsonAsync<AdminProgramDetailDto>(
            new Uri($"/api/admin/programs/{created.Id}", UriKind.Relative), TestJson.Options);
        fetched!.RegistrationFields.ClassLetter.Should().Be("Required", "jsonb ustunda saqlanib, GetById'da qayta o'qilishi kerak");
        fetched.RegistrationFields.ParentPhone.Should().Be("Required");
    }

    /// <summary>
    /// Qat'iy invariant (egasining talabi): shaxsiyat batareyasi bor dasturda `birthDate`/`grade`
    /// `Required`dan boshqasiga o'rnatib bo'lmaydi — BIRINCHI nazorat nuqtasi (`SetRegistrationFields`).
    /// `gender`ni MAJBURIY QILISH esa batareyali dasturda ham muvaffaqiyatli bo'lishi kerak.
    /// </summary>
    [Fact]
    public async Task Update_OptionalBirthDate_BatareyaliDasturda_400REGISTRATION_FIELD_REQUIRED_FOR_BATTERYQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "REGFIELDSBAT1", 1);

        using var client = await AuthenticatedClientAsync("programs-regfields-battery-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGFIELDS-BAT-1", nameUz = "Batareyali dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/admin/programs/{created!.Id}/tests", new { testDefinitionId = mbtiTest.Id, displayOrder = 1 }, TestJson.Options);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/programs/{created.Id}",
            new
            {
                nameUz = created.NameUz,
                descriptionUz = (string?)null,
                displayOrder = 1,
                visibility = "Assigned",
                registrationFields = new { birthDate = "Optional" },
            },
            TestJson.Options);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_FIELD_REQUIRED_FOR_BATTERY");

        var afterFailedUpdate = await client.GetFromJsonAsync<AdminProgramDetailDto>(
            new Uri($"/api/admin/programs/{created.Id}", UriKind.Relative), TestJson.Options);
        afterFailedUpdate!.RegistrationFields.BirthDate.Should().Be("Required", "muvaffaqiyatsiz urinish holatni o'zgartirmasligi kerak");
    }

    [Fact]
    public async Task Update_RequiredGender_BatareyaliDasturdaHamMuvaffaqiyatliBoLadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "REGFIELDSGEN1", 1);

        using var client = await AuthenticatedClientAsync("programs-regfields-gender-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new { code = "REGFIELDS-GEN-1", nameUz = "Jins majburiy dastur", descriptionUz = (string?)null, displayOrder = 1, visibility = "Assigned" },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/admin/programs/{created!.Id}/tests", new { testDefinitionId = mbtiTest.Id, displayOrder = 1 }, TestJson.Options);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/programs/{created.Id}",
            new
            {
                nameUz = created.NameUz,
                descriptionUz = (string?)null,
                displayOrder = 1,
                visibility = "Assigned",
                registrationFields = new { gender = "Required" },
            },
            TestJson.Options);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);
        updated!.RegistrationFields.Gender.Should().Be("Required");
    }

    /// <summary>IKKINCHI nazorat nuqtasi — `Publish`da ham qulflangan (`SetRegistrationFields`ni chetlab o'tib bo'lmaydi).</summary>
    [Fact]
    public async Task Publish_OptionalBirthDateBatareyaliDastur_400REGISTRATION_FIELD_REQUIRED_FOR_BATTERYQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var mbtiTest = await TestDataFactory.CreateStandaloneSystemMbtiShapedTestAsync(db, now, "REGFIELDSPUB1", 1);

        using var client = await AuthenticatedClientAsync("programs-regfields-publish-admin");
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/programs",
            new
            {
                code = "REGFIELDS-PUB-1",
                nameUz = "Nashr sinovi dasturi",
                descriptionUz = (string?)null,
                displayOrder = 1,
                visibility = "Assigned",
                registrationFields = new { grade = "Optional" },
            },
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProgramDetailDto>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/admin/programs/{created!.Id}/tests", new { testDefinitionId = mbtiTest.Id, displayOrder = 1 }, TestJson.Options);

        var publishResponse = await client.PostAsync(new Uri($"/api/admin/programs/{created.Id}/publish", UriKind.Relative), content: null);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await publishResponse.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_FIELD_REQUIRED_FOR_BATTERY");
    }
}

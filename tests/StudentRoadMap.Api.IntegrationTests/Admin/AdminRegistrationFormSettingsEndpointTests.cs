using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Settings.RegistrationForm;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// GLOBAL ro'yxatdan o'tish formasi sozlamasi (2026-09-11/12, egasining talabi, `docs/18` §9.6) —
/// `GET`/`PUT /api/admin/settings/registration-form`. Alohida `IClassFixture` — boshqa admin
/// login testlari bilan rate limiter (10/5 daqiqa/IP) bo'linib ketmasligi uchun
/// (`AdminProgramRegistrationFieldsEndpointTests` uslubi).
/// </summary>
public sealed class AdminRegistrationFormSettingsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminRegistrationFormSettingsEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<StudentRoadMap.Application.Common.Interfaces.IPasswordHasher>();
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
    public async Task Get_SozlamaHaliSaqlanmagan_StandartQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("regform-get-default-admin");

        var response = await client.GetAsync(new Uri("/api/admin/settings/registration-form", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<RegistrationFormDefinitionDto>(TestJson.Options);
        dto!.CoreFields.FullName.Requirement.Should().Be("Required");
        dto.CoreFields.BirthDate.Requirement.Should().Be("Required");
        dto.CoreFields.Gender.Requirement.Should().Be("Required");
        dto.CoreFields.Grade.Requirement.Should().Be("Required");
        dto.CoreFields.ClassLetter.Requirement.Should().Be("Optional");
        dto.CoreFields.Phone.Requirement.Should().Be("Required");
        dto.CoreFields.ParentPhone.Requirement.Should().Be("Optional");
        dto.CoreFields.Email.Requirement.Should().Be("Optional");
        dto.CustomFields.Should().BeEmpty();
    }

    private static object ValidCoreFieldsBody() => new
    {
        fullName = new { requirement = "Required", labelUz = "F.I.Sh.", placeholderUz = (string?)null, order = 1 },
        birthDate = new { requirement = "Required", labelUz = "Tug'ilgan sana", placeholderUz = (string?)null, order = 2 },
        gender = new { requirement = "Required", labelUz = "Jins", placeholderUz = (string?)null, order = 3 },
        grade = new { requirement = "Required", labelUz = "Sinf", placeholderUz = (string?)null, order = 4 },
        classLetter = new { requirement = "Optional", labelUz = "Sinf harfi", placeholderUz = (string?)null, order = 5 },
        phone = new { requirement = "Required", labelUz = "Telefon raqami", placeholderUz = (string?)null, order = 6 },
        parentPhone = new { requirement = "Optional", labelUz = "Ota-ona telefoni", placeholderUz = (string?)null, order = 7 },
        email = new { requirement = "Optional", labelUz = "Email", placeholderUz = (string?)null, order = 8 },
    };

    [Fact]
    public async Task Put_ToLiqAlmashtiradiVaAuditYozadi()
    {
        using var client = await AuthenticatedClientAsync("regform-put-admin");

        var body = new
        {
            coreFields = ValidCoreFieldsBody(),
            customFields = new[]
            {
                new
                {
                    code = "PARENT_JOB", type = "ShortText", labelUz = "Ota-onangiz kasbi",
                    placeholderUz = "Masalan: o'qituvchi", requirement = "Optional",
                    maxLength = 200, inputPattern = (string?)null, options = (object?)null, order = 9,
                },
            },
        };

        var response = await client.PutAsJsonAsync("/api/admin/settings/registration-form", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<RegistrationFormDefinitionDto>(TestJson.Options);
        dto!.CustomFields.Should().ContainSingle(f => f.Code == "PARENT_JOB" && f.Type == "ShortText");

        var getResponse = await client.GetAsync(new Uri("/api/admin/settings/registration-form", UriKind.Relative));
        var fetched = await getResponse.Content.ReadFromJsonAsync<RegistrationFormDefinitionDto>(TestJson.Options);
        fetched!.CustomFields.Should().ContainSingle(f => f.Code == "PARENT_JOB", "jsonb ustunda saqlanib, GET'da qayta o'qilishi kerak");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.AuditLogs.AsNoTracking().AnyAsync(a => a.Action == AuditActions.RegistrationFormSettingsUpdated)).Should().BeTrue();
    }

    [Fact]
    public async Task Put_FullNameniOzgartirishgaUrinish_400Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("regform-fullname-lock-admin");

        var body = new
        {
            coreFields = new
            {
                fullName = new { requirement = "Optional", labelUz = "F.I.Sh.", placeholderUz = (string?)null, order = 1 },
                birthDate = new { requirement = "Required", labelUz = "Tug'ilgan sana", placeholderUz = (string?)null, order = 2 },
                gender = new { requirement = "Required", labelUz = "Jins", placeholderUz = (string?)null, order = 3 },
                grade = new { requirement = "Required", labelUz = "Sinf", placeholderUz = (string?)null, order = 4 },
                classLetter = new { requirement = "Optional", labelUz = "Sinf harfi", placeholderUz = (string?)null, order = 5 },
                phone = new { requirement = "Required", labelUz = "Telefon raqami", placeholderUz = (string?)null, order = 6 },
                parentPhone = new { requirement = "Optional", labelUz = "Ota-ona telefoni", placeholderUz = (string?)null, order = 7 },
                email = new { requirement = "Optional", labelUz = "Email", placeholderUz = (string?)null, order = 8 },
            },
            customFields = Array.Empty<object>(),
        };

        var response = await client.PutAsJsonAsync("/api/admin/settings/registration-form", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_FORM_FULL_NAME_LOCKED");
    }

    [Fact]
    public async Task Put_TakroriyMaydonKodi_409Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("regform-dup-code-admin");

        var body = new
        {
            coreFields = ValidCoreFieldsBody(),
            customFields = new[]
            {
                new { code = "DUP", type = "ShortText", labelUz = "Birinchi", placeholderUz = (string?)null, requirement = "Optional", maxLength = 200, inputPattern = (string?)null, options = (object?)null, order = 9 },
                new { code = "DUP", type = "ShortText", labelUz = "Ikkinchi", placeholderUz = (string?)null, requirement = "Optional", maxLength = 200, inputPattern = (string?)null, options = (object?)null, order = 10 },
            },
        };

        var response = await client.PutAsJsonAsync("/api/admin/settings/registration-form", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_FORM_FIELD_CODE_DUPLICATE");
    }

    [Fact]
    public async Task Put_TanlovMaydonidaIkkitadanKamVariant_400Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("regform-choice-options-admin");

        var body = new
        {
            coreFields = ValidCoreFieldsBody(),
            customFields = new[]
            {
                new
                {
                    code = "CHOICE", type = "SingleChoice", labelUz = "Tanlov", placeholderUz = (string?)null,
                    requirement = "Optional", maxLength = (int?)null, inputPattern = (string?)null,
                    options = new[] { new { textUz = "Bitta", value = "1", order = 1 } },
                    order = 9,
                },
            },
        };

        var response = await client.PutAsJsonAsync("/api/admin/settings/registration-form", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT");
    }

    [Fact]
    public async Task Put_InputPatternKompilyatsiyaQilinmasa_400INPUT_PATTERN_INVALIDQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("regform-bad-pattern-admin");

        var body = new
        {
            coreFields = ValidCoreFieldsBody(),
            customFields = new[]
            {
                new
                {
                    code = "BADPATTERN", type = "ShortText", labelUz = "Test", placeholderUz = (string?)null,
                    requirement = "Optional", maxLength = 200, inputPattern = "(unterminated[", options = (object?)null, order = 9,
                },
            },
        };

        var response = await client.PutAsJsonAsync("/api/admin/settings/registration-form", body, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("INPUT_PATTERN_INVALID");
    }

    [Fact]
    public async Task Get_TokenSizYuborilsa_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/settings/registration-form", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

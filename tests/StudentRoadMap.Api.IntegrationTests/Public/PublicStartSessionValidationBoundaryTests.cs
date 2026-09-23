using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Public;

/// <summary>
/// `StartSessionCommandValidator` chegara qoidalari — `prompts/10` tuzatish #2: F.I.Sh. &lt; 5
/// belgi, yosh chegaralari (5/6/20/21), sinf chegaralari (0/1/11/12). Bitta fixture ichida
/// jami 9 ta `POST /api/public/sessions` so'rovi yuboriladi — `RateLimitSetup.PublicStartSession`
/// (10/soat) chegarasidan ATAYLAB past, boshqa testlarga (masalan `PublicStartSessionEndpointTests`)
/// aralashmasligi uchun bu ALOHIDA `IClassFixture&lt;PublicApiTestFactory&gt;` nusxasida.
/// </summary>
public sealed class PublicStartSessionValidationBoundaryTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public PublicStartSessionValidationBoundaryTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task<(School School, string AccessToken)> SeedSchoolAsync(string seed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var token = TestDataFactory.NewAccessToken(seed);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-{seed}", token);

        if (!await db.TestDefinitions.AnyAsync())
        {
            await TestDataFactory.CreatePublishedTestAsync(db, now, "VBND1", 1, questionCount: 1);
        }

        return (school, token);
    }

    private static StartSessionCommand ValidCommand(string slug, string token, string fullName, DateOnly birthDate, int grade = 9) =>
        new(
            Slug: slug,
            AccessToken: token,
            AccessCode: null,
            FullName: fullName,
            BirthDate: birthDate,
            Gender: Gender.Male,
            Grade: grade,
            ClassLetter: "B",
            Phone: "+998901234567",
            ParentPhone: "+998909998877",
            Email: null,
            ConsentAccepted: true,
            LanguageCode: "uz");

    /// <summary>`StartSessionCommandValidator`: F.I.Sh. kamida 5 belgi.</summary>
    [Fact]
    public async Task StartSession_FishBeshBelgidanKam_400VaFullNameXatoQaytaradi()
    {
        var (school, token) = await SeedSchoolAsync("fish-invalid1");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Abcd", new DateOnly(2010, 1, 1));

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty("fullName", out _).Should().BeTrue();
    }

    /// <summary>`StartSessionCommandValidator`: tug'ilgan sana 6-20 yosh oralig'idan tashqarida (5, 21 yosh) — 400.</summary>
    [Theory]
    [InlineData(5)]
    [InlineData(21)]
    public async Task StartSession_YoshChegaradanTashqarida_400VaBirthDateXatoQaytaradi(int age)
    {
        var (school, token) = await SeedSchoolAsync($"age-invalid-{age}");
        using var client = _factory.CreateClient();

        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-age);
        var command = ValidCommand(school.Slug.Value, token, "Test Foydalanuvchi Yoshi", birthDate);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty("birthDate", out _).Should().BeTrue();
    }

    /// <summary>`StartSessionCommandValidator`: 6 va 20 yosh — chegara qiymatlari RUXSAT ETILGAN (inclusive).</summary>
    [Theory]
    [InlineData(6)]
    [InlineData(20)]
    public async Task StartSession_YoshChegaraQiymatlarida_201Qaytaradi(int age)
    {
        var (school, token) = await SeedSchoolAsync($"age-valid-{age}");
        using var client = _factory.CreateClient();

        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-age);
        var command = ValidCommand(school.Slug.Value, token, "Test Foydalanuvchi Chegarasi", birthDate);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>`StartSessionCommandValidator`: sinf 1-11 oralig'idan tashqarida (0, 12) — 400.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task StartSession_SinfChegaradanTashqarida_400VaGradeXatoQaytaradi(int grade)
    {
        var (school, token) = await SeedSchoolAsync($"grade-invalid-{grade}");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Test Foydalanuvchi Sinfi", new DateOnly(2010, 1, 1), grade);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        problem.GetProperty("errors").TryGetProperty("grade", out _).Should().BeTrue();
    }

    /// <summary>`StartSessionCommandValidator`: 1 va 11-sinf — chegara qiymatlari RUXSAT ETILGAN (inclusive).</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public async Task StartSession_SinfChegaraQiymatlarida_201Qaytaradi(int grade)
    {
        var (school, token) = await SeedSchoolAsync($"grade-valid-{grade}");
        using var client = _factory.CreateClient();

        var command = ValidCommand(school.Slug.Value, token, "Test Foydalanuvchi Sinfvalid", new DateOnly(2010, 1, 1), grade);

        var response = await client.PostAsJsonAsync("/api/public/sessions", command, TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

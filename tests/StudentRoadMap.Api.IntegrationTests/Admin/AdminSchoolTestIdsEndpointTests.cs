using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// Maktab formasida endi DASTUR emas, TESTLAR tanlanadi (2026-09-23 egasi qarori, `docs/07`
/// §3.2, `docs/18` §9.7): `POST`/`PUT /api/admin/schools` `testIds` qabul qiladi, detal javobi
/// `testIds` qaytaradi; ichkarida har test o'z test dasturiga map qilinadi.
/// </summary>
public sealed class AdminSchoolTestIdsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSchoolTestIdsEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_TestIdsBilan_TestlarBiriktiriladi_VaLandingdaKorinadi()
    {
        var testA = await CreatePublishedTestAsync("SCH-TEST-A");
        var testB = await CreatePublishedTestAsync("SCH-TEST-B");
        using var client = await AuthenticatedClientAsync("unused");

        var response = await client.PostAsJsonAsync(
            "/api/admin/schools",
            new { name = "Testli maktab", region = "Toshkent", district = "Mirobod", testIds = new[] { testA.Id, testB.Id } },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var school = (await response.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;
        school.TestIds.Should().BeEquivalentTo(new[] { testA.Id, testB.Id });

        // Test dasturlari yaratildi — har testga bittadan.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.AssessmentPrograms.CountAsync(p => p.OwnerTestDefinitionId == testA.Id)).Should().Be(1);
            (await db.AssessmentPrograms.CountAsync(p => p.OwnerTestDefinitionId == testB.Id)).Should().Be(1);
        }

        // GET ham xuddi shu ro'yxatni qaytaradi.
        var detail = await client.GetFromJsonAsync<AdminSchoolDetailDto>($"/api/admin/schools/{school.Id}", TestJson.Options);
        detail!.TestIds.Should().BeEquivalentTo(new[] { testA.Id, testB.Id });

        // Ommaviy landing: ikkala test ham (test nomi bilan) ko'rinadi.
        string accessToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            accessToken = (await db.Schools.SingleAsync(s => s.Id == school.Id)).AccessToken;
        }

        using var anonymous = _factory.CreateClient();
        var landing = await anonymous.GetFromJsonAsync<JsonElement>($"/api/public/schools/{school.Slug}?k={accessToken}");
        var programs = landing.GetProperty("programs").EnumerateArray().ToList();
        programs.Should().Contain(p => p.GetProperty("code").GetString() == testA.Code && p.GetProperty("nameUz").GetString() == testA.NameUz);
        programs.Should().Contain(p => p.GetProperty("code").GetString() == testB.Code && p.GetProperty("nameUz").GetString() == testB.NameUz);
    }

    [Fact]
    public async Task Update_TestIds_ToLiqAlmashtiriladi_NullBoLsaOzgarmaydi()
    {
        var testA = await CreatePublishedTestAsync("SCH-UPD-A");
        var testB = await CreatePublishedTestAsync("SCH-UPD-B");
        var testC = await CreatePublishedTestAsync("SCH-UPD-C");
        using var client = await AuthenticatedClientAsync("unused");

        var created = await client.PostAsJsonAsync(
            "/api/admin/schools",
            new { name = "Yangilanadigan maktab", region = "Toshkent", district = "Sergeli", testIds = new[] { testA.Id, testB.Id } },
            TestJson.Options);
        var school = (await created.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!;

        var replaced = await client.PutAsJsonAsync(
            $"/api/admin/schools/{school.Id}",
            new { name = "Yangilanadigan maktab", region = "Toshkent", district = "Sergeli", testIds = new[] { testB.Id, testC.Id } },
            TestJson.Options);
        replaced.StatusCode.Should().Be(HttpStatusCode.OK);
        (await replaced.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!.TestIds
            .Should().BeEquivalentTo(new[] { testB.Id, testC.Id });

        // `testIds` yuborilmasa — biriktirmalar o'zgarmaydi.
        var untouched = await client.PutAsJsonAsync(
            $"/api/admin/schools/{school.Id}",
            new { name = "Nomi o'zgardi", region = "Toshkent", district = "Sergeli" },
            TestJson.Options);
        untouched.StatusCode.Should().Be(HttpStatusCode.OK);
        (await untouched.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!.TestIds
            .Should().BeEquivalentTo(new[] { testB.Id, testC.Id });

        // Bo'sh ro'yxat — hammasi olib tashlanadi; test dasturlari (tarix) qoladi.
        var cleared = await client.PutAsJsonAsync(
            $"/api/admin/schools/{school.Id}",
            new { name = "Nomi o'zgardi", region = "Toshkent", district = "Sergeli", testIds = Array.Empty<Guid>() },
            TestJson.Options);
        (await cleared.Content.ReadFromJsonAsync<AdminSchoolDetailDto>(TestJson.Options))!.TestIds.Should().BeEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.AssessmentPrograms.CountAsync(p => p.OwnerTestDefinitionId == testA.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Create_NomaLumTest_404_MaktabYaratilmaydi()
    {
        using var client = await AuthenticatedClientAsync("unused");

        var response = await client.PostAsJsonAsync(
            "/api/admin/schools",
            new { name = "Xato maktab", region = "Toshkent", district = "Olmazor", testIds = new[] { Guid.NewGuid() } },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Schools.AnyAsync(s => s.Name == "Xato maktab")).Should().BeFalse();
    }

    private async Task<TestDefinition> CreatePublishedTestAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await TestDataFactory.CreateStandaloneTestAsync(db, DateTimeOffset.UtcNow, code, displayOrder: 1);
    }

    /// <summary>
    /// Login rate limiti (`RateLimitSetup`) sinf ichida ko'p test uchun bitta token ishlatishni
    /// talab qiladi — token fikstura (factory) bo'yicha bir marta olinadi.
    /// </summary>
    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static (PublicApiTestFactory Factory, string Token)? _cachedToken;

    private async Task<HttpClient> AuthenticatedClientAsync(string username)
    {
        _ = username;
        var token = await GetTokenAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> GetTokenAsync()
    {
        await TokenLock.WaitAsync();
        try
        {
            if (_cachedToken is { } cached && ReferenceEquals(cached.Factory, _factory))
            {
                return cached.Token;
            }

            const string adminUsername = "school-testids-admin";
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                await AdminTestDataFactory.CreateAdminUserAsync(db, hasher, DateTimeOffset.UtcNow, adminUsername);
            }

            using var client = _factory.CreateClient();
            var loginResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = adminUsername, password = AdminTestDataFactory.DefaultPassword },
                TestJson.Options);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>(TestJson.Options))!;

            _cachedToken = (_factory, login.AccessToken);
            return login.AccessToken;
        }
        finally
        {
            TokenLock.Release();
        }
    }
}

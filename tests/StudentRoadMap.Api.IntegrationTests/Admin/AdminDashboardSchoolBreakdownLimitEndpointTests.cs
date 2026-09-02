using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Dashboard;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `schoolBreakdown` — DB darajasidagi `ORDER BY` + `LIMIT 20` (`docs/07` 3.6-bo'lim,
/// `prompts/15` vazifa 2 cheklovi: "MAKSIMUM 20 qator", "Xotirada saralash ... taqiqlanadi").
/// Alohida `IClassFixture` — `AdminDashboardFunnelEndpointTests`/`AdminDashboardStatsEndpointTests`
/// bilan kvota/ma'lumot BAHAM KO'RILMAYDI (`prompts/15` "Yangi test klasslarini alohida
/// IClassFixture'ga qo'y").
/// </summary>
public sealed class AdminDashboardSchoolBreakdownLimitEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private const int TotalSchools = 21; // 20 dan BITTA ko'p — chegara chindan ISHLASHINI isbotlash uchun.

    private readonly PublicApiTestFactory _factory;

    public AdminDashboardSchoolBreakdownLimitEndpointTests(PublicApiTestFactory factory)
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
    public async Task GetStats_YigirmaBirMaktab_EngFaoliYigirmatasiVaToGriTartibdaQaytadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var windowFrom = now.AddDays(-1);
        var windowTo = now.AddDays(1);

        var phoneCounter = 0;
        var schoolIdsByRegistered = new Dictionary<int, Guid>();

        // Har bir maktabga ANIQ, BOSHQA-BOSHQA sonli o'quvchi (1..21) — saralash tartibini
        // ANIQ tekshirish uchun (tenglik/tie-break YO'Q).
        for (var registered = 1; registered <= TotalSchools; registered++)
        {
            var token = TestDataFactory.NewAccessToken($"limit-{registered}");
            var school = await TestDataFactory.CreateSchoolAsync(db, now, $"maktab-limit-{registered:00}", token);
            schoolIdsByRegistered[registered] = school.Id;

            for (var i = 0; i < registered; i++)
            {
                phoneCounter++;
                var student = Student.Create(
                    Guid.NewGuid(), school.Id, $"Talaba {registered}-{i}", new DateOnly(2010, 1, 1), Gender.Male, 9,
                    PhoneNumber.Create($"+998{phoneCounter:000000000}").Value, now, now);
                db.Students.Add(student);
            }

            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientAsync("dashboard-limit-admin");

        var stats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?from={Uri.EscapeDataString(windowFrom.ToString("O"))}&to={Uri.EscapeDataString(windowTo.ToString("O"))}",
            TestJson.Options);

        stats.Should().NotBeNull();

        // Chegara — MAKSIMUM 20, 21 emas.
        stats!.SchoolBreakdown.Should().HaveCount(20);

        // Tartib — kamayish bo'yicha ANIQ (eng faoli, ya'ni ko'proq `registered`, BIRINCHI).
        stats.SchoolBreakdown.Select(s => s.Registered).Should().BeInDescendingOrder();
        stats.SchoolBreakdown[0].Registered.Should().Be(21);
        stats.SchoolBreakdown[0].SchoolId.Should().Be(schoolIdsByRegistered[21]);

        // Eng zaif (registered=1) chegaradan TASHQARIDA qolishi kerak.
        stats.SchoolBreakdown.Should().NotContain(s => s.SchoolId == schoolIdsByRegistered[1]);

        // Qaytgan 20 ta — aynan registered=21..2 bo'lgan maktablar (registered=1 kesilgan).
        stats.SchoolBreakdown.Select(s => s.SchoolId).Should().BeEquivalentTo(
            Enumerable.Range(2, 20).Select(r => schoolIdsByRegistered[r]));
    }
}

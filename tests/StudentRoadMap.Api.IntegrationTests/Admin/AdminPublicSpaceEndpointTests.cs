using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.PublicSpace;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `PublicSpaceController` — ommaviy makonning ALOHIDA admin bo'limi (2026-09-06).
///
/// Bu yerda qulflanadigan asosiy shartlar:
/// <list type="bullet">
///   <item>bo'lim maktab endpointlaridan mustaqil ishlaydi (`/api/admin/public-space`);</item>
///   <item>dastur O'CHIRILGAN bo'lsa javob buni ANIQ ko'rsatadi (`availability.status` va
///   `programs[].isActive`) — hozirgi jonli holat aynan shu, va panel bu haqda jim qolmasligi
///   kerak edi (2026-09-03 hodisasi);</item>
///   <item>makonni o'chirish/faolsizlantirish endpointi UMUMAN YO'Q.</item>
/// </list>
/// </summary>
public sealed class AdminPublicSpaceEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminPublicSpaceEndpointTests(PublicApiTestFactory factory)
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

    private async Task<School> SeedSpaceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, DateTimeOffset.UtcNow);
    }

    /// <summary>Nashr qilingan, LEKIN `IsActive = false` dastur — jonli holatning nusxasi.</summary>
    private async Task<Guid> CreateDeactivatedProgramAsync(string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        // `Publish` kamida bitta test biriktirilishini talab qiladi (domen invarianti).
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, now, $"{code}-TEST", displayOrder: 1);

        var program = AssessmentProgram.Create(Guid.NewGuid(), code, "O'chirilgan dastur (sinov)", now, visibility: ProgramVisibility.Assigned);
        program.AddTest(testDefinition.Id, 1, now);
        program.Publish(now);
        program.Deactivate(now);
        db.AssessmentPrograms.Add(program);
        await db.SaveChangesAsync();
        db.Entry(program).State = EntityState.Detached;

        return program.Id;
    }

    [Fact]
    public async Task Get_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/public-space", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_MakonHolatiVaStatistikasiniQaytaradi()
    {
        var space = await SeedSpaceAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;

            var user = Student.Create(
                Guid.NewGuid(), space.Id, "Ommaviy Foydalanuvchi Testovich", new DateOnly(2005, 5, 5),
                Gender.Male, 11, PhoneNumber.Create("+998905550001").Value, now, now);
            db.Students.Add(user);
            await db.SaveChangesAsync();

            var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
            db.Assessments.Add(Assessment.Create(
                Guid.NewGuid(), user.Id, space.Id, "public-space-stats-token-0001", "uz", programId,
                startedAt: now, expiresAt: now.AddDays(7), now: now));
            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientAsync("public-space-get-admin");

        var dto = await client.GetFromJsonAsync<AdminPublicSpaceDto>("/api/admin/public-space", TestJson.Options);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(space.Id);
        dto.Slug.Should().Be("ommaviy");
        dto.IsActive.Should().BeTrue();
        dto.ShowResultToStudent.Should().BeTrue("ommaviy makonda foydalanuvchi o'z natijasini ko'rmasa kabinetning ma'nosi qolmaydi");
        dto.PublicUrl.Should().EndWith("/kirish", "ommaviy oqim maxfiy havolaga emas, kirish sahifasiga tayanadi");
        dto.Stats.UserCount.Should().BeGreaterThanOrEqualTo(1);
        dto.Stats.TotalAssessments.Should().BeGreaterThanOrEqualTo(1);
        dto.Availability.Should().NotBeNull();
    }

    [Fact]
    public async Task AssignVaUnassignProgram_BiriktirmaniOzgartiradi()
    {
        await SeedSpaceAsync();
        var programId = await CreateDeactivatedProgramAsync("PUBLIC-SPACE-ASSIGN-TEST");

        using var client = await AuthenticatedClientAsync("public-space-assign-admin");

        var afterAssign = await (await client.PostAsync(
                new Uri($"/api/admin/public-space/programs/{programId}", UriKind.Relative), content: null))
            .Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options);

        afterAssign.Should().NotBeNull();
        afterAssign!.Programs.Should().Contain(p => p.Id == programId);

        // O'CHIRILGAN dastur: panel buni jim yutmasligi kerak (2026-09-03 hodisasi).
        var assigned = afterAssign.Programs.Single(p => p.Id == programId);
        assigned.State.Should().Be(nameof(ProgramState.Paused));

        var afterUnassign = await (await client.DeleteAsync(
                new Uri($"/api/admin/public-space/programs/{programId}", UriKind.Relative)))
            .Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options);

        afterUnassign.Should().NotBeNull();
        afterUnassign!.Programs.Should().NotContain(p => p.Id == programId);
    }

    [Fact]
    public async Task AssignProgram_MavjudEmasDastur_404Qaytaradi()
    {
        await SeedSpaceAsync();

        using var client = await AuthenticatedClientAsync("public-space-assign-404-admin");

        var response = await client.PostAsync(
            new Uri($"/api/admin/public-space/programs/{Guid.NewGuid()}", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetShowResult_BayroqniOzgartiradiVaSaqlaydi()
    {
        var space = await SeedSpaceAsync();

        using var client = await AuthenticatedClientAsync("public-space-showresult-admin");

        var disabled = await (await client.PutAsJsonAsync(
                "/api/admin/public-space/show-result", new { enabled = false }, TestJson.Options))
            .Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options);

        disabled.Should().NotBeNull();
        disabled!.ShowResultToStudent.Should().BeFalse();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.Schools.AsNoTracking().FirstAsync(s => s.Id == space.Id);
            stored.ShowResultToStudent.Should().BeFalse("o'zgarish bazaga yozilishi kerak, faqat javobda emas");
        }

        var enabled = await (await client.PutAsJsonAsync(
                "/api/admin/public-space/show-result", new { enabled = true }, TestJson.Options))
            .Content.ReadFromJsonAsync<AdminPublicSpaceDto>(TestJson.Options);

        enabled!.ShowResultToStudent.Should().BeTrue();
    }

    [Fact]
    public async Task OchirishVaFaolsizlantirishEndpointlari_MavjudEmas()
    {
        await SeedSpaceAsync();

        using var client = await AuthenticatedClientAsync("public-space-protected-admin");

        // Domen `SCHOOL_PUBLIC_SPACE_PROTECTED` bilan taqiqlaydi — endpointning O'ZI ham
        // yo'q, ya'ni admin bunday amalni umuman topa olmaydi (`PublicSpaceController` izohi).
        var delete = await client.DeleteAsync(new Uri("/api/admin/public-space", UriKind.Relative));
        delete.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);

        var toggle = await client.PostAsync(new Uri("/api/admin/public-space/toggle-active", UriKind.Relative), content: null);
        toggle.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `AssessmentsController.Delete` — `docs/07` 3.3-bo'lim: soft delete. Alohida `IClassFixture`.
/// </summary>
public sealed class AdminAssessmentsDeleteEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminAssessmentsDeleteEndpointTests(PublicApiTestFactory factory)
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
    public async Task Delete_MavjudEmas_404Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("assessments-delete-404-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/assessments/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_MavjudSessiya_YumshoqOChiradiVaAuditYozadi_ShaxsiyMalumotsiz()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "assess-delete-a", TestDataFactory.NewAccessToken("assess-delete-a"));
        var phone = PhoneNumber.Create("+998907775001").Value;
        var student = Student.Create(Guid.NewGuid(), school.Id, "Delete Test Talabasi Maxfiyovna", new DateOnly(2010, 1, 1), Gender.Female, 9, phone, now, now);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "assess-delete-session-0123456789ab", "uz", now, now.AddDays(7), now);
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("assessments-delete-admin");

        var response = await client.DeleteAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Global filtr orqali endi "ko'rinmaydi" (`IsDeleted = true`), lekin fizik o'chmagan.
            (await verifyDb.Assessments.AnyAsync(a => a.Id == assessment.Id)).Should().BeFalse();
            var raw = await verifyDb.Assessments.IgnoreQueryFilters().SingleAsync(a => a.Id == assessment.Id);
            raw.IsDeleted.Should().BeTrue();

            var auditLog = await verifyDb.AuditLogs.AsNoTracking()
                .SingleAsync(a => a.Action == AuditActions.AssessmentDeleted && a.EntityId == assessment.Id);
            auditLog.AfterJson.Should().NotContain("Delete Test Talabasi Maxfiyovna");
            auditLog.AfterJson.Should().NotContain("+998907775001");
        }

        // Endi GetById 404 qaytarishi kerak (global filtr orqali "topilmaydi").
        var getResponse = await client.GetAsync(new Uri($"/api/admin/assessments/{assessment.Id}", UriKind.Relative));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

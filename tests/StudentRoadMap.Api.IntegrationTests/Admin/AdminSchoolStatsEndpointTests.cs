using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `GET /api/admin/schools/{id}` — maktab ichki sahifasidagi ISHTIROK STATISTIKASI
/// (`docs/07` 3.1-bo'lim). Mantiq `GetSchoolByIdQueryHandlerTests`da DB'siz sinaladi; bu yerda
/// EF Core darajasi tekshiriladi: so'rovlar tarjima qilinadimi va `IsDeleted` GLOBAL QUERY
/// FILTRI (`AppDbContext.OnModelCreating`) haqiqatan ham qo'llanadimi.
///
/// Alohida `IClassFixture` — rate limiter kvotasi boshqa admin test klasslaridan
/// aralashmasligi uchun (`AdminSchoolsListEndpointTests` izohidagi bilan bir xil sabab).
/// </summary>
public sealed class AdminSchoolStatsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSchoolStatsEndpointTests(PublicApiTestFactory factory)
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
    public async Task GetById_IshtirokStatistikasi_ToGriQaytariladi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(
            db, now, "maktab-stats-detail", TestDataFactory.NewAccessToken("stats-detail"));

        // 4 ta o'quvchi: 2 tasi yakunlagan, 2 tasi yo'q → completionRate = 0.5 (ULUSH, foiz emas).
        var lastActivity = now.AddDays(-1);
        await AddStudentAsync(db, school.Id, "Yakunlagan Bir", "+998911110001", now, completedCount: 1, lastAssessmentAt: now.AddDays(-3));
        await AddStudentAsync(db, school.Id, "Yakunlagan Ikki", "+998911110002", now, completedCount: 2, lastAssessmentAt: lastActivity);
        var inProgressStudent = await AddStudentAsync(db, school.Id, "Jarayonda Bir", "+998911110003", now, completedCount: 0, lastAssessmentAt: null);
        await AddStudentAsync(db, school.Id, "Boshlamagan Bir", "+998911110004", now, completedCount: 0, lastAssessmentAt: null);

        // Soft-delete qilingan o'quvchi — global filtr uni HISOBGA OLMASLIGI kerak.
        var deletedStudent = await AddStudentAsync(db, school.Id, "Ochirilgan Bir", "+998911110005", now, completedCount: 1, lastAssessmentAt: now);
        deletedStudent.MarkDeleted(now);
        await db.SaveChangesAsync();

        // Jarayondagi sessiya (`InProgress`) + hali boshlanmagan (`Draft`) — faqat birinchisi sanaladi.
        await CreateAssessmentAsync(db, school, inProgressStudent, "stats-session-inprogress-0123456789ab", now.AddMinutes(-30), "STAT-A", start: true);
        await CreateAssessmentAsync(db, school, inProgressStudent, "stats-session-draft-0123456789abcd", now.AddMinutes(-20), "STAT-B", start: false);

        using var client = await AuthenticatedClientAsync("school-stats-admin");

        var detail = await client.GetFromJsonAsync<AdminSchoolDetailDto>(
            $"/api/admin/schools/{school.Id}", TestJson.Options);

        detail.Should().NotBeNull();
        detail!.Stats.StudentCount.Should().Be(4, "soft-delete qilingan o'quvchi sanalmaydi");
        detail.Stats.CompletedCount.Should().Be(2);
        detail.Stats.InProgressCount.Should().Be(1, "faqat `Status == InProgress` (Draft emas)");
        detail.Stats.CompletionRate.Should().Be(0.5, "ULUSH (0..1), foiz EMAS — frontend `× 100` qiladi");
        detail.Stats.LastActivityAt.Should().NotBeNull();
        detail.Stats.LastActivityAt!.Value.Should().BeCloseTo(lastActivity, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetById_OquvchisizMaktab_NolVaNullQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(
            db, now, "maktab-stats-bosh", TestDataFactory.NewAccessToken("stats-bosh"));

        using var client = await AuthenticatedClientAsync("school-stats-empty-admin");

        var detail = await client.GetFromJsonAsync<AdminSchoolDetailDto>(
            $"/api/admin/schools/{school.Id}", TestJson.Options);

        detail!.Stats.StudentCount.Should().Be(0);
        detail.Stats.CompletedCount.Should().Be(0);
        detail.Stats.InProgressCount.Should().Be(0);
        detail.Stats.CompletionRate.Should().BeNull("`registered == 0` — nisbat ANIQLANMAGAN, `0` EMAS");
        detail.Stats.LastActivityAt.Should().BeNull();
    }

    private static async Task<Student> AddStudentAsync(
        AppDbContext db, Guid schoolId, string fullName, string phone, DateTimeOffset now, int completedCount, DateTimeOffset? lastAssessmentAt)
    {
        var student = Student.Create(
            Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Male, 9,
            PhoneNumber.Create(phone).Value, now, now);

        if (lastAssessmentAt.HasValue)
        {
            student.UpdateSnapshot(null, null, null, null, null, false, lastAssessmentAt.Value, completedCount, now);
        }

        db.Students.Add(student);
        await db.SaveChangesAsync();

        return student;
    }

    /// <summary>`start: true` → `InProgress`, aks holda `Draft` holatida qoladi.</summary>
    private static async Task<Assessment> CreateAssessmentAsync(
        AppDbContext db, School school, Student student, string sessionToken, DateTimeOffset startedAt, string testCodeSeed, bool start)
    {
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, startedAt, testCodeSeed, displayOrder: 1, questionCount: 1);
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, startedAt);

        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId,
            startedAt: startedAt, expiresAt: startedAt.AddDays(7), now: startedAt);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        if (start)
        {
            assessment.StartTest(testDefinition.Id, startedAt);
        }

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        return assessment;
    }
}

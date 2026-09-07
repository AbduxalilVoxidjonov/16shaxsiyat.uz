using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `GET /api/admin/students` filtrlari — `gender`, `ageMin`/`ageMax` (2026-09-07, egasining
/// talabi: "yosh, jins, holat, aktivlik darajasi"), `status`, `activityLevel` — va ularning
/// validatsiyasi (`ListStudentsQueryValidator`). Yosh chegaralari `StudentAgeRange`
/// formulalari bilan HTTP darajasida qulflanadi: bugun tug'ilgan kuni bo'lgan o'quvchi
/// `ageMin`ga KIRADI, bugun `ageMax + 1` yoshga to'lgani esa KIRMAYDI. Alohida
/// `IClassFixture` (rate limiter kvotasi, boshqa Admin testlar bilan bir xil naqsh).
/// </summary>
public sealed class AdminStudentsFilterEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminStudentsFilterEndpointTests(PublicApiTestFactory factory)
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

    private static Student MakeStudent(Guid schoolId, DateTimeOffset now, string fullName, string phone, Gender gender = Gender.Male, DateOnly? birthDate = null) =>
        Student.Create(Guid.NewGuid(), schoolId, fullName, birthDate ?? new DateOnly(2010, 1, 1), gender, 9, PhoneNumber.Create(phone).Value, now, now);

    private static async Task<HashSet<Guid>> ListIdsAsync(HttpClient client, string queryString)
    {
        var result = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            $"/api/admin/students?{queryString}&pageSize=100", TestJson.Options);
        return result!.Items.Select(s => s.Id).ToHashSet();
    }

    [Fact]
    public async Task List_GenderFiltri_FaqatTanlanganJinsniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-gender", TestDataFactory.NewAccessToken("students-gender"));
        var male = MakeStudent(school.Id, now, "Gender Erkak Oquvchi", "+998903210001", Gender.Male);
        var female = MakeStudent(school.Id, now, "Gender Ayol Oquvchi", "+998903210002", Gender.Female);
        var unspecified = MakeStudent(school.Id, now, "Gender Korsatilmagan Oquvchi", "+998903210003", Gender.Unspecified);
        db.Students.AddRange(male, female, unspecified);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-gender-admin");

        var males = await ListIdsAsync(client, $"schoolId={school.Id}&gender=Male");
        males.Should().BeEquivalentTo([male.Id]);

        // Katta-kichik harf farqsiz — frontend `Female` yuboradi, qo'lda yozilgan `female` ham ishlaydi.
        var females = await ListIdsAsync(client, $"schoolId={school.Id}&gender=female");
        females.Should().BeEquivalentTo([female.Id]);

        // Filtrsiz — uchalasi ham (jins ko'rsatilmagan o'quvchi yo'qolib qolmaydi).
        var all = await ListIdsAsync(client, $"schoolId={school.Id}");
        all.Should().BeEquivalentTo([male.Id, female.Id, unspecified.Id]);
    }

    [Fact]
    public async Task List_YoshFiltri_ChegaraHolatlari_BugunTugilganKuniHisobgaOlinadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-age", TestDataFactory.NewAccessToken("students-age"));

        // `ageMin=11&ageMax=14` oralig'i uchun to'rtta chegara o'quvchisi:
        var turns11Today = MakeStudent(school.Id, now, "Yosh Bugun On Bir", "+998903220001", birthDate: today.AddYears(-11));               // 11 — KIRADI
        var turns11Tomorrow = MakeStudent(school.Id, now, "Yosh Ertaga On Bir", "+998903220002", birthDate: today.AddYears(-11).AddDays(1)); // hali 10 — KIRMAYDI
        var turns15Today = MakeStudent(school.Id, now, "Yosh Bugun On Besh", "+998903220003", birthDate: today.AddYears(-15));              // 15 — KIRMAYDI
        var turns15Tomorrow = MakeStudent(school.Id, now, "Yosh Ertaga On Besh", "+998903220004", birthDate: today.AddYears(-15).AddDays(1)); // hali 14 — KIRADI
        db.Students.AddRange(turns11Today, turns11Tomorrow, turns15Today, turns15Tomorrow);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-age-admin");

        var inRange = await ListIdsAsync(client, $"schoolId={school.Id}&ageMin=11&ageMax=14");
        inRange.Should().BeEquivalentTo([turns11Today.Id, turns15Tomorrow.Id]);

        // Faqat quyi chegara — 11 va undan kattalar (15 yoshli ham).
        var atLeast11 = await ListIdsAsync(client, $"schoolId={school.Id}&ageMin=11");
        atLeast11.Should().BeEquivalentTo([turns11Today.Id, turns15Today.Id, turns15Tomorrow.Id]);

        // Faqat yuqori chegara — 14 va undan kichiklar (10 yoshli ham).
        var atMost14 = await ListIdsAsync(client, $"schoolId={school.Id}&ageMax=14");
        atMost14.Should().BeEquivalentTo([turns11Today.Id, turns11Tomorrow.Id, turns15Tomorrow.Id]);
    }

    [Fact]
    public async Task List_StatusFiltri_FaqatShuHolatdagiSessiyasiBorOquvchilarniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-status", TestDataFactory.NewAccessToken("students-status"));
        var draftStudent = MakeStudent(school.Id, now, "Status Qoralama Oquvchi", "+998903230001");
        var inProgressStudent = MakeStudent(school.Id, now, "Status Jarayonda Oquvchi", "+998903230002");
        var noSessionStudent = MakeStudent(school.Id, now, "Status Sessiyasiz Oquvchi", "+998903230003");
        db.Students.AddRange(draftStudent, inProgressStudent, noSessionStudent);
        await db.SaveChangesAsync();

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, now, "students-status", displayOrder: 1, questionCount: 1);

        var draft = Assessment.Create(Guid.NewGuid(), draftStudent.Id, school.Id, "students-status-draft-token-000001", "uz", programId, startedAt: now, expiresAt: now.AddDays(7), now: now);

        var inProgress = Assessment.Create(Guid.NewGuid(), inProgressStudent.Id, school.Id, "students-status-progress-token-01", "uz", programId, startedAt: now, expiresAt: now.AddDays(7), now: now);
        inProgress.AddTest(AssessmentTest.Create(Guid.NewGuid(), inProgress.Id, testDefinition.Id, 1, totalCount: 1));
        inProgress.StartTest(testDefinition.Id, now);

        db.Assessments.AddRange(draft, inProgress);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-status-admin");

        var drafts = await ListIdsAsync(client, $"schoolId={school.Id}&status=Draft");
        drafts.Should().BeEquivalentTo([draftStudent.Id]);

        var inProgressIds = await ListIdsAsync(client, $"schoolId={school.Id}&status=InProgress");
        inProgressIds.Should().BeEquivalentTo([inProgressStudent.Id]);

        var analyzed = await ListIdsAsync(client, $"schoolId={school.Id}&status=Analyzed");
        analyzed.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ActivityLevelFiltri_BeshDarajaningHarBiriAlohidaIshlaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var school = await TestDataFactory.CreateSchoolAsync(db, now, "students-activity", TestDataFactory.NewAccessToken("students-activity"));

        var levels = Enum.GetValues<ActivityLevel>();
        var studentByLevel = new Dictionary<ActivityLevel, Student>();
        foreach (var level in levels)
        {
            var student = MakeStudent(school.Id, now, $"Aktivlik {level} Oquvchi", $"+99890324000{(int)level}");
            student.UpdateSnapshot(null, null, 50.0, level, null, needsAttention: false, lastAssessmentAt: now, completedAssessmentCount: 1, now: now);
            studentByLevel[level] = student;
        }

        var noLevel = MakeStudent(school.Id, now, "Aktivlik Yoq Oquvchi", "+998903240009");
        db.Students.AddRange(studentByLevel.Values.Append(noLevel));
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("students-activity-admin");

        foreach (var level in levels)
        {
            var ids = await ListIdsAsync(client, $"schoolId={school.Id}&activityLevel={level}");
            ids.Should().BeEquivalentTo([studentByLevel[level].Id], "`activityLevel={0}` faqat shu darajadagi o'quvchini qaytarishi kerak", level);
        }
    }

    /// <summary>
    /// Bitta `Fact` ichida barcha noto'g'ri holatlar — `AdminLogin` limiti 10/5 daqiqa (IP
    /// bo'yicha, `RateLimitSetup`), har holat uchun alohida login fixtura kvotasini tugatardi.
    /// </summary>
    [Fact]
    public async Task ListVaExport_NotoGriGenderYokiYosh_400ValidationErrorQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("students-filter-validation-admin");

        string[] invalidQueries =
        [
            "gender=Unspecified",
            "gender=maktab",
            "gender=1",
            "ageMin=5",
            "ageMax=100",
            "ageMin=15&ageMax=11",
        ];

        foreach (var queryString in invalidQueries)
        {
            var response = await client.GetAsync(new Uri($"/api/admin/students?{queryString}", UriKind.Relative));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "`?{0}` validatsiyadan o'tmasligi kerak", queryString);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
            problem!.Extensions.Should().ContainKey("code");
            problem.Extensions["code"]!.ToString().Should().Be("VALIDATION_ERROR");

            // Eksport ro'yxat bilan AYNAN bir xil validatsiyadan o'tadi (`ExportStudentsQueryValidator`).
            var exportResponse = await client.GetAsync(new Uri($"/api/admin/students/export?{queryString}", UriKind.Relative));
            exportResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "eksport `?{0}` ni ham rad etishi kerak", queryString);
        }

        // Chegara qiymatlari (6 va 99, `ageMin == ageMax`) — TO'G'RI.
        var boundary = await client.GetAsync(new Uri("/api/admin/students?gender=Female&ageMin=6&ageMax=99", UriKind.Relative));
        boundary.StatusCode.Should().Be(HttpStatusCode.OK);
        var equal = await client.GetAsync(new Uri("/api/admin/students?ageMin=12&ageMax=12", UriKind.Relative));
        equal.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Dashboard;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `DashboardController.GetStats` — `funnel`/`schoolBreakdown` kengaytmasi (`docs/07` 3.6-bo'lim,
/// `prompts/15` vazifa 2, 2026-09-02). Alohida `IClassFixture` (rate limiter kvotasi,
/// `prompts/15` "Yangi test klasslarini alohida IClassFixture'ga qo'y") —
/// `AdminDashboardStatsEndpointTests` bilan kvota BAHAM KO'RILMAYDI.
///
/// **Faqat BITTA test metodi** haqiqiy vaqt (`DateTimeOffset.UtcNow`) oynasini kesib o'tadigan
/// `Student`/`Assessment` qo'shadi (`AdminDashboardStatsEndpointTests` klassidagi bilan bir xil
/// intizom — `Student.CreatedAt` DOIM haqiqiy tizim soatiga o'rnatiladi, shu sabab bir nechta
/// shunday metod bir-birining natijasiga aralashib ketardi). `schoolBreakdown` 20-qatorlik
/// chegara/tartib sinovi ALOHIDA klassda (`AdminDashboardSchoolBreakdownLimitEndpointTests`).
/// </summary>
public sealed class AdminDashboardFunnelEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminDashboardFunnelEndpointTests(PublicApiTestFactory factory)
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

    private static Student MakeStudent(Guid schoolId, DateTimeOffset now, string fullName, string phone) =>
        Student.Create(Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Male, 9, PhoneNumber.Create(phone).Value, now, now);

    /// <summary>Draft/InProgress/Completed/Analyzed — 4 bosqichdan BIRIGA olib boradigan yordamchi (`stage` bilan boshqariladi).</summary>
    private static async Task<Assessment> CreateAssessmentAtStageAsync(
        AppDbContext db, School school, Student student, string sessionToken, DateTimeOffset startedAt, string testCodeSeed, string stage)
    {
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, startedAt, testCodeSeed, displayOrder: 1, questionCount: 1);
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, startedAt);

        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: startedAt, expiresAt: startedAt.AddDays(7), now: startedAt);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        if (stage != "Draft")
        {
            var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == testDefinition.Id)).Id;
            assessment.StartTest(testDefinition.Id, startedAt);

            if (stage != "InProgress")
            {
                assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, startedAt);
                assessment.CompleteTest(testDefinition.Id, [questionId], startedAt.AddMinutes(5));
                assessment.Complete(startedAt.AddMinutes(5));

                if (stage == "Analyzed")
                {
                    assessment.MarkAnalyzing(startedAt.AddMinutes(6));
                    assessment.MarkAnalyzed(startedAt.AddMinutes(7));
                }
                else if (stage == "SoftDeletedCompleted")
                {
                    assessment.MarkDeleted(startedAt.AddMinutes(6));
                }
            }
        }

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        return assessment;
    }

    [Fact]
    public async Task GetStats_ToliqVoronkaStsenariysi_FunnelVaSchoolBreakdownToGriHisoblanadi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        var windowFrom = now.AddHours(-2);
        var windowTo = now.AddHours(2);

        var activeToken = TestDataFactory.NewAccessToken("funnel-active");
        var activeSchool = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-funnel-active", activeToken);

        // Nol faollikdagi maktab — `registered = 0` bo'lganda `completionRate = null` (nol
        // bo'linishsiz) ekanini isbotlash uchun (`prompts/15` vazifa 2 talab qilingan test).
        // `Schools` asosiy jadval bo'lgani uchun (Students/Assessments bo'yicha GroupBy EMAS)
        // bu maktab HECH QANDAY o'quvchi/sessiyasiz ham `schoolBreakdown`da ko'rinishi kerak.
        var zeroToken = TestDataFactory.NewAccessToken("funnel-zero");
        var zeroSchool = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-funnel-zero", zeroToken);

        // 1) Faqat ro'yxatdan o'tgan (Draft) — `registered`ga kiradi, `started`ga KIRMAYDI.
        var draftStudent = MakeStudent(activeSchool.Id, now, "Draft Talaba", "+998900000001");
        db.Students.Add(draftStudent);
        await db.SaveChangesAsync();
        await CreateAssessmentAtStageAsync(db, activeSchool, draftStudent, "funnel-session-draft-0123456789ab", now.AddMinutes(-90), "FUN-A", "Draft");

        // 2) Boshlangan (InProgress) — `started`ga kiradi, `completed`ga KIRMAYDI.
        var inProgressStudent = MakeStudent(activeSchool.Id, now, "InProgress Talaba", "+998900000002");
        db.Students.Add(inProgressStudent);
        await db.SaveChangesAsync();
        await CreateAssessmentAtStageAsync(db, activeSchool, inProgressStudent, "funnel-session-inprogress-0123456789ab", now.AddMinutes(-80), "FUN-B", "InProgress");

        // 3) Yakunlangan (Completed) — `completed`ga kiradi, `analyzed`ga KIRMAYDI.
        var completedStudent = MakeStudent(activeSchool.Id, now, "Completed Talaba", "+998900000003");
        db.Students.Add(completedStudent);
        await db.SaveChangesAsync();
        await CreateAssessmentAtStageAsync(db, activeSchool, completedStudent, "funnel-session-completed-0123456789ab", now.AddMinutes(-70), "FUN-C", "Completed");

        // 4) To'liq tahlil qilingan (Analyzed) — 5 bosqichning HAMMASIGA kiradi.
        var analyzedStudent = MakeStudent(activeSchool.Id, now, "Analyzed Talaba", "+998900000004");
        db.Students.Add(analyzedStudent);
        await db.SaveChangesAsync();
        await CreateAssessmentAtStageAsync(db, activeSchool, analyzedStudent, "funnel-session-analyzed-0123456789ab", now.AddMinutes(-60), "FUN-D", "Analyzed");

        // 5) YAKUNLANGAN, lekin SOFT O'CHIRILGAN — global filtr orqali `started`/`completed`/
        //    `analyzed`ning HECH BIRIGA kirmasligi kerak (`Status` "Completed" bo'lsa ham).
        //    `registered`ga ESA kiradi — chunki O'QUVCHI o'chirilmagan, faqat SESSIYA.
        var deletedStudent = MakeStudent(activeSchool.Id, now, "Ochirilgan Talaba", "+998900000005");
        db.Students.Add(deletedStudent);
        await db.SaveChangesAsync();
        await CreateAssessmentAtStageAsync(db, activeSchool, deletedStudent, "funnel-session-deleted-0123456789abc", now.AddMinutes(-50), "FUN-E", "SoftDeletedCompleted");

        // Havola ochilishi — `linkViews` uchun (bitta HTTP mijoz, 2 marta).
        using var publicClient = _factory.CreateClient();
        (await publicClient.GetAsync(new Uri($"/api/public/schools/{activeSchool.Slug.Value}?k={activeToken}", UriKind.Relative)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await publicClient.GetAsync(new Uri($"/api/public/schools/{activeSchool.Slug.Value}?k={activeToken}", UriKind.Relative)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        using var client = await AuthenticatedClientAsync("dashboard-funnel-admin");

        var stats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?from={Uri.EscapeDataString(windowFrom.ToString("O"))}&to={Uri.EscapeDataString(windowTo.ToString("O"))}",
            TestJson.Options);

        stats.Should().NotBeNull();

        // --- Voronka (global, ikkala maktab bo'ylab) ---
        stats!.Funnel.LinkViews.Should().Be(2);
        stats.Funnel.Registered.Should().Be(5); // 5 ta o'quvchi yaratildi (soft o'chirilgan sessiyaning o'quvchisi ham kiradi).
        stats.Funnel.Started.Should().Be(3); // InProgress/Completed/Analyzed (Draft va soft o'chirilgan KIRMAYDI).
        stats.Funnel.Completed.Should().Be(2); // Completed va Analyzed (soft o'chirilgan KIRMAYDI).
        stats.Funnel.Analyzed.Should().Be(1); // Faqat Analyzed.

        // --- Maktab kesimi ---
        stats.SchoolBreakdown.Should().HaveCount(2);

        var activeRow = stats.SchoolBreakdown.Should().ContainSingle(s => s.SchoolId == activeSchool.Id).Subject;
        activeRow.Name.Should().Be(activeSchool.Name);
        activeRow.Region.Should().Be(activeSchool.Region);
        activeRow.LinkViews.Should().Be(2);
        activeRow.Registered.Should().Be(5);
        activeRow.Completed.Should().Be(2);
        activeRow.CompletionRate.Should().Be(0.4); // 2 / 5
        activeRow.LastActivityAt.Should().NotBeNull();

        var zeroRow = stats.SchoolBreakdown.Should().ContainSingle(s => s.SchoolId == zeroSchool.Id).Subject;
        zeroRow.LinkViews.Should().Be(0);
        zeroRow.Registered.Should().Be(0);
        zeroRow.Completed.Should().Be(0);
        zeroRow.CompletionRate.Should().BeNull("registered = 0 bo'lganda nisbat ANIQLANMAGAN — 0% emas (PM tuzatmasi, 2026-09-02)");
        zeroRow.LastActivityAt.Should().BeNull();

        // Eng faoli (ko'proq `registered`) BIRINCHI kelishi kerak.
        stats.SchoolBreakdown[0].SchoolId.Should().Be(activeSchool.Id);
        stats.SchoolBreakdown[1].SchoolId.Should().Be(zeroSchool.Id);
    }
}

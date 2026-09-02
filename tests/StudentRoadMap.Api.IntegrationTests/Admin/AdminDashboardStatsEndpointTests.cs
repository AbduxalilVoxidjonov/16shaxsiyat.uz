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
/// `DashboardController.GetStats` — `docs/07` 3.6-bo'lim, `prompts/15`. Alohida `IClassFixture`
/// (rate limiter kvotasi, `prompts/15` "Yangi test klasslarini alohida IClassFixture'ga qo'y").
///
/// **SQLite `DateTimeOffset` cheklovi — YOPILDI** (`prompts/15` ikkinchi bosqichi, 2026-09-02):
/// `AppDbContext.ApplySqliteDateTimeOffsetConversion` endi SQLite'da `DateTimeOffset`ni `long`
/// (UTC tick) ga o'giradi — `WHERE`/`ORDER BY` endi DB darajasida ishlaydi, avvalgi `Skip`lar
/// olib tashlandi.
///
/// **Testlar orasidagi izolatsiya:** bu klass `IClassFixture` orqali BITTA (baham ko'rilgan)
/// SQLite bazasini barcha test metodlari bilan bo'lishadi (rate limiter kvotasi sababli, loyiha
/// konvensiyasi). `Totals` (butun jadval bo'yicha, oynasiz) shu sabab BOSHQA test metodi
/// qo'shgan yozuvlarni ham ko'radi — mutlaq songa emas, DELTA (oldin/keyin farqi)ga tekshiriladi.
/// `last30Days`/`recentAssessments` esa — o'zga testlar bilan HECH QACHON kesishmaydigan aniq
/// vaqt oynalari (tarixiy "epoch" yoki kelajakdagi bo'sh oyna) bilan izolyatsiya qilinadi.
/// </summary>
public sealed class AdminDashboardStatsEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminDashboardStatsEndpointTests(PublicApiTestFactory factory)
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

    /// <summary>
    /// Haqiqiy domen oqimi orqali (reflection/xom SQL YO'Q) — bitta savolli anketa, `StartTest`
    /// → `UpsertAnswer` → `CompleteTest` → `Complete(completedAt)` (davomiylik shu orqali aniq
    /// nazorat qilinadi) → `SetReliability`. `AdminStudentGetByIdEndpointTests`dagi bilan bir xil naqsh.
    /// </summary>
    private static async Task<Assessment> CreateCompletedAssessmentAsync(
        AppDbContext db,
        School school,
        Student student,
        string sessionToken,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        double reliabilityScore,
        string testCodeSeed)
    {
        var testDefinition = await TestDataFactory.CreatePublishedTestAsync(db, startedAt, testCodeSeed, displayOrder: 1, questionCount: 1);
        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, startedAt);

        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, sessionToken, "uz", programId, startedAt: startedAt, expiresAt: startedAt.AddDays(7), now: startedAt);
        var assessmentTest = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);
        assessment.AddTest(assessmentTest);

        var questionId = (await db.Questions.AsNoTracking().FirstAsync(q => q.TestDefinitionId == testDefinition.Id)).Id;
        assessment.StartTest(testDefinition.Id, startedAt);
        assessmentTest.UpsertAnswer(Guid.NewGuid(), questionId, 4, null, 2000, startedAt);
        assessment.CompleteTest(testDefinition.Id, [questionId], completedAt);
        assessment.Complete(completedAt);
        assessment.SetReliability(reliabilityScore, ReliabilityFlag.Reliable, completedAt);

        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();

        return assessment;
    }

    [Fact]
    public async Task GetStats_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/dashboard/stats", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_ToLiqStsenariy_ToGriHisoblanadiVaOChirilganlarHisobgaOlinmaydi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // `Totals` (butun jadval, oynasiz) — sinf ichidagi BOSHQA test metodlari (`IClassFixture`
        // — baham ko'rilgan baza) ham maktab/o'quvchi qo'shishi mumkin (ish tartibi kafolatlanmagan,
        // xUnit); shu sabab mutlaq songa emas, shu testdan OLDINGI holatga nisbatan DELTAga
        // tekshiramiz.
        var schoolsBefore = await db.Schools.CountAsync();
        var activeSchoolsBefore = await db.Schools.CountAsync(s => s.IsActive);
        var studentsBefore = await db.Students.CountAsync();
        var needsAttentionBefore = await db.Students.CountAsync(s => s.NeedsAttention);
        var completedAssessmentsBefore = await db.Assessments.CountAsync(a => a.CompletedAt != null);

        // `Assessment.StartedAt`/`CompletedAt` erkin belgilanadi (domen orqali), lekin
        // `Student.CreatedAt` DOIM `AppDbContext.UpdateAuditFields` orqali HAQIQIY tizim
        // soatiga o'rnatiladi (audit maydoni, domendan qat'i nazar) — shu sabab bu test
        // tarixiy "epoch" EMAS, HAQIQIY `now` bilan ishlaydi. Bu xavfsiz: shu klassdagi BOSHQA
        // test metodlari (`GetStats_BoshOynadaMalumotYoq_...`/`GetStats_IkkiKetmaKetSoRov_...`)
        // FAQAT `School` qo'shadi (`Student`/`Assessment` YO'Q) — shu sabab `last30Days`
        // (`Student.CreatedAt`/`Assessment.StartedAt`ga bog'liq) ularning ta'siriga uchramaydi;
        // faqat `Totals.Schools`/`ActiveSchools` uchun yuqoridagi DELTA yetarli.
        var now = DateTimeOffset.UtcNow;
        var windowFrom = now.AddDays(-30);
        var windowTo = now.AddDays(1);

        var activeSchool = await TestDataFactory.CreateSchoolAsync(db, now, "dash-active", TestDataFactory.NewAccessToken("dash-active"));
        var inactiveSchool = await TestDataFactory.CreateSchoolAsync(db, now, "dash-inactive", TestDataFactory.NewAccessToken("dash-inactive"), isActive: false);

        // 1) Yakunlangan sessiya (30 daqiqa, ishonchlilik 82) — statistikaga kiradi.
        var completedStudent = MakeStudent(activeSchool.Id, now, "Karimova Sevinch Baxtiyorovna", "+998901234501");
        completedStudent.UpdateSnapshot("INTJ", 70.0, 80.0, ActivityLevel.Active, "IRA", needsAttention: false, lastAssessmentAt: now, completedAssessmentCount: 1, now: now);
        db.Students.Add(completedStudent);
        await db.SaveChangesAsync();

        var completedAssessment = await CreateCompletedAssessmentAsync(
            db, activeSchool, completedStudent, "dash-session-completed-0123456789ab",
            startedAt: now.AddMinutes(-40), completedAt: now.AddMinutes(-10), reliabilityScore: 82.0, testCodeSeed: "DASH-A");

        // 2) Boshlangan, lekin YAKUNLANMAGAN sessiya (`InProgress`) — `dropOffRate`ga kiradi.
        var inProgressStudent = MakeStudent(activeSchool.Id, now, "Tursunov Aziz Davronovich", "+998901234502");
        db.Students.Add(inProgressStudent);
        await db.SaveChangesAsync();
        var inProgressTest = await TestDataFactory.CreatePublishedTestAsync(db, now, "DASH-B", displayOrder: 1, questionCount: 1);
        var inProgressProgramId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var inProgressAssessment = Assessment.Create(
            Guid.NewGuid(), inProgressStudent.Id, activeSchool.Id, "dash-session-inprogress-0123456789ab", "uz", inProgressProgramId,
            startedAt: now.AddMinutes(-20), expiresAt: now.AddDays(7), now: now);
        var inProgressAssessmentTest = AssessmentTest.Create(Guid.NewGuid(), inProgressAssessment.Id, inProgressTest.Id, 1, totalCount: 1);
        inProgressAssessment.AddTest(inProgressAssessmentTest);
        inProgressAssessment.StartTest(inProgressTest.Id, now.AddMinutes(-20));
        db.Assessments.Add(inProgressAssessment);
        await db.SaveChangesAsync();

        // 3) E'tibor talab qiladigan o'quvchi (needsAttention) — `totals.needsAttention`ga kiradi.
        var attentionStudent = MakeStudent(activeSchool.Id, now, "Yusupova Malika Sherzodovna", "+998901234503");
        attentionStudent.UpdateSnapshot(null, null, 15.0, ActivityLevel.Passive, null, needsAttention: true, lastAssessmentAt: now, completedAssessmentCount: 1, now: now);
        db.Students.Add(attentionStudent);
        await db.SaveChangesAsync();

        // 4) SOFT O'CHIRILGAN sessiya — global filtr orqali statistikada HISOBGA OLINMASLIGI kerak
        //    (`prompts/15` cheklov: "Statistikada o'chirilgan yozuvlar hisobga olinmaydi").
        var deletedStudent = MakeStudent(activeSchool.Id, now, "Ochirilgan Talaba", "+998901234504");
        db.Students.Add(deletedStudent);
        await db.SaveChangesAsync();
        var deletedAssessment = await CreateCompletedAssessmentAsync(
            db, activeSchool, deletedStudent, "dash-session-deleted-0123456789abc",
            startedAt: now.AddMinutes(-50), completedAt: now.AddMinutes(-45), reliabilityScore: 99.0, testCodeSeed: "DASH-C");
        deletedAssessment.MarkDeleted(now);
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("dashboard-admin");

        var stats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?from={Uri.EscapeDataString(windowFrom.ToString("O"))}&to={Uri.EscapeDataString(windowTo.ToString("O"))}",
            TestJson.Options);

        stats.Should().NotBeNull();

        // Totals — DELTA: 2 ta yangi maktab (1 faol), 4 ta yangi o'quvchi, 1 ta yangi needsAttention.
        stats!.Totals.Schools.Should().Be(schoolsBefore + 2);
        stats.Totals.ActiveSchools.Should().Be(activeSchoolsBefore + 1);
        stats.Totals.Students.Should().Be(studentsBefore + 4);
        stats.Totals.NeedsAttention.Should().Be(needsAttentionBefore + 1);

        // `completedAssessments` — faqat SOFT O'CHIRILMAGAN yakunlangan sessiya (+1, o'chirilgani hisobga olinmaydi).
        stats.Totals.CompletedAssessments.Should().Be(completedAssessmentsBefore + 1);

        // `last30Days` — `ScenarioEpoch` atrofidagi oyna boshqa testlar ("now" ~2026) bilan
        // HECH QACHON kesishmaydi, shu sabab bu yerda MUTLAQ songa tekshirish xavfsiz.
        // 2 ta sessiya oynada boshlangan (o'chirilgani global filtr bilan chiqarib tashlanadi),
        // 1 tasi yakunlangan → dropOffRate = 1/2 = 0.5.
        stats.Last30Days.Completed.Should().Be(1);
        stats.Last30Days.DropOffRate.Should().Be(0.5);
        stats.Last30Days.AvgDurationMinutes.Should().Be(30.0); // 40 daqiqadan boshlanib 10 daqiqa qolganda yakunlangan
        stats.Last30Days.AvgReliability.Should().Be(82.0);
        stats.Last30Days.NewStudents.Should().Be(4);

        stats.PersonalityDistribution.Should().ContainSingle(p => p.Type == "INTJ" && p.Count == 1);
        stats.ActivityDistribution.Should().Contain(a => a.Level == "Active" && a.Count == 1);
        stats.HollandTop.Should().ContainSingle(h => h.Code == "IRA" && h.Count == 1);

        stats.RecentAssessments.Should().ContainSingle(a => a.AssessmentId == completedAssessment.Id);
        stats.RecentAssessments.Should().NotContain(a => a.AssessmentId == deletedAssessment.Id);
    }

    [Fact]
    public async Task GetStats_BoshOynadaMalumotYoq_NolBolinishsizIshlaydi()
    {
        using var client = await AuthenticatedClientAsync("dashboard-empty-admin");

        // Boshqa testlar bilan HECH QACHON kesishmaydigan, kelajakdagi bo'sh oyna — standart
        // (`from`/`to`siz) so'rov boshqa test metodlari qo'shgan "hozirgi" ma'lumotni ushlab
        // qolishi mumkin edi (baham ko'rilgan baza, `IClassFixture`).
        var farFrom = DateTimeOffset.UtcNow.AddYears(50);
        var farTo = farFrom.AddDays(1);

        var stats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?from={Uri.EscapeDataString(farFrom.ToString("O"))}&to={Uri.EscapeDataString(farTo.ToString("O"))}",
            TestJson.Options);

        stats.Should().NotBeNull();
        stats!.Last30Days.NewStudents.Should().Be(0);
        stats.Last30Days.Completed.Should().Be(0);
        stats.Last30Days.DropOffRate.Should().Be(0.0);
        stats.Last30Days.AvgDurationMinutes.Should().Be(0.0);
        stats.Last30Days.AvgReliability.Should().Be(0.0);
    }

    [Fact]
    public async Task GetStats_IkkiKetmaKetSoRov_KeshlanganNatijaniQaytaradi()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Baham ko'rilgan bazadagi boshqa testlar allaqachon maktab qo'shgan bo'lishi mumkin —
        // mutlaq son emas, shu testdan OLDINGI holatga nisbatan DELTA tekshiriladi.
        var schoolsBefore = await db.Schools.CountAsync();

        var now = DateTimeOffset.UtcNow;
        var school = await TestDataFactory.CreateSchoolAsync(db, now, "dash-cache", TestDataFactory.NewAccessToken("dash-cache"));
        await db.SaveChangesAsync();

        using var client = await AuthenticatedClientAsync("dashboard-cache-admin");

        // Bir xil `from`/`to` (ANIQ, ISO-8601) — kesh kaliti bir xil bo'lishi uchun.
        var from = now.AddDays(-30).ToString("O");
        var to = now.ToString("O");
        var url = $"/api/admin/dashboard/stats?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";

        var first = await client.GetFromJsonAsync<AdminDashboardStatsDto>(url, TestJson.Options);
        first!.Totals.Schools.Should().Be(schoolsBefore + 1);

        // Keshdan KEYIN yangi maktab qo'shiladi — kesh 60 soniya ISHLASA, ikkinchi so'rov HAM
        // eski (o'zgarmagan) qiymatni qaytarishi kerak (DB'da endi yana bitta ko'p bo'lsa ham).
        using (var mutateScope = _factory.Services.CreateScope())
        {
            var mutateDb = mutateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.CreateSchoolAsync(mutateDb, now, "dash-cache-2", TestDataFactory.NewAccessToken("dash-cache-2"));
        }

        var second = await client.GetFromJsonAsync<AdminDashboardStatsDto>(url, TestJson.Options);
        second!.Totals.Schools.Should().Be(
            schoolsBefore + 1, "60 soniyalik kesh ikkinchi so'rovda ham eski natijani qaytarishi kerak");
    }

    /// <summary>
    /// ⚠️ QA topilmasi (2026-09-02, BLOKLOVCHI, tuzatildi): AVVAL kesh kaliti HISOBLANGAN
    /// `from`/`to`dan (`?? _dateTime.UtcNow` bilan to'ldirilgan) qurilardi — PARAMETRSIZ
    /// so'rovda (`GET /api/admin/dashboard/stats`, dashboard'ning ENG KO'P ishlatiladigan —
    /// odatiy — yuklanishi) `to` har chaqiruvda TIK aniqligida boshqacha chiqib, kesh HECH
    /// QACHON urmasdi. Yuqoridagi `GetStats_IkkiKetmaKetSoRov_...` testi buni USHLAMAYDI,
    /// chunki u ikkala chaqiruvda ATAYLAB BIR XIL `from`/`to`ni qotirib beradi — aynan
    /// ishlamaydigan (parametrsiz) yo'lni chetlab o'tadi. Bu test — QA talab qilgan aniq
    /// isbot: PARAMETRSIZ (`from`/`to`SIZ) ikki ketma-ket chaqiruv, orasida ma'lumot qo'shib.
    /// </summary>
    [Fact]
    public async Task GetStats_ParametrsizIkkiKetmaKetSoRov_KeshlanganNatijaniQaytaradi()
    {
        using var client = await AuthenticatedClientAsync("dashboard-cache-noparams-admin");

        // Birinchi (parametrsiz) chaqiruv — natijani ("eski holat") 60 soniyaga keshlaydi.
        var first = await client.GetFromJsonAsync<AdminDashboardStatsDto>("/api/admin/dashboard/stats", TestJson.Options);
        first.Should().NotBeNull();
        var schoolsAtFirstCall = first!.Totals.Schools;

        // Keshdan KEYIN yangi maktab qo'shiladi.
        using (var mutateScope = _factory.Services.CreateScope())
        {
            var mutateDb = mutateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await TestDataFactory.CreateSchoolAsync(mutateDb, DateTimeOffset.UtcNow, "dash-cache-noparams", TestDataFactory.NewAccessToken("dash-cache-noparams"));
        }

        // Ikkinchi (yana PARAMETRSIZ) chaqiruv — kesh ISHLASA, YANGI maktabni HALI KO'RMASLIGI
        // kerak (birinchi chaqiruvdagi bilan BIR XIL — eski — qiymatni qaytaradi).
        var second = await client.GetFromJsonAsync<AdminDashboardStatsDto>("/api/admin/dashboard/stats", TestJson.Options);
        second.Should().NotBeNull();
        second!.Totals.Schools.Should().Be(
            schoolsAtFirstCall,
            "kesh parametrsiz so'rovda ham ishlashi shart — ikkinchi chaqiruv yangi maktabni HALI ko'rmasligi kerak");
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.Assessments;
using StudentRoadMap.Application.Admin.Dashboard;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// **Manba (`?source=`) bo'yicha ajratish** — o'quvchilar/sessiyalar ro'yxati va boshqaruv
/// paneli (2026-09-06, `AdminSourceFilter`).
///
/// <para>
/// Muammoning ildizi: ommaviy makon `schools` jadvalidagi qator bo'lgani uchun uning
/// foydalanuvchilari va sessiyalari admin ro'yxatlarida oddiy maktab qatorlari bilan
/// ARALASHIB ketardi — `schoolName` ustuni ikki oqimni ajratmasdi. Endi har qator o'z
/// `source` qiymatini olib yuradi va ro'yxat/panel manba bo'yicha filtrlanadi.
/// </para>
/// <para>
/// Dashboard uchun eng muhim qulf: `?source=school` javobiga ommaviy foydalanuvchilar
/// QO'SHILMAYDI va aksincha — aks holda egasi "maktablarda 40 o'quvchi bor" degan raqamni
/// ko'rib, aslida ularning hammasi Telegram orqali kirgan tashqi foydalanuvchi bo'lardi.
/// </para>
/// </summary>
public sealed class AdminSourceFilterEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    public AdminSourceFilterEndpointTests(PublicApiTestFactory factory)
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

    /// <summary>Ikki oqim: maktabda 1 o'quvchi+sessiya, ommaviy makonda 1 foydalanuvchi+sessiya.</summary>
    private async Task<(Guid SchoolStudentId, Guid PublicStudentId, Guid SchoolAssessmentId, Guid PublicAssessmentId)> SeedBothFlowsAsync(
        string seed, string phonePrefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var space = await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);
        var school = await TestDataFactory.CreateSchoolAsync(db, now, seed, TestDataFactory.NewAccessToken(seed));

        // Ism `seed` bilan UNIKAL: ommaviy makon BARCHA testlar orasida bir xil yozuv, va
        // `ux_students_school_normalized_name_birth_date` bir xil ism+sanani ikkinchi marta
        // yozishga yo'l qo'ymaydi (`IClassFixture` bitta bazani baham ko'radi).
        var schoolStudent = MakeStudent(school.Id, now, $"Maktab Oquvchisi {seed}", $"{phonePrefix}01");
        var publicStudent = MakeStudent(space.Id, now, $"Ommaviy Foydalanuvchi {seed}", $"{phonePrefix}02");
        db.Students.AddRange(schoolStudent, publicStudent);
        await db.SaveChangesAsync();

        var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);
        var schoolAssessment = MakeAssessment(schoolStudent.Id, school.Id, now, $"{seed}-school-token-000001", programId);
        var publicAssessment = MakeAssessment(publicStudent.Id, space.Id, now, $"{seed}-public-token-000001", programId);
        db.Assessments.AddRange(schoolAssessment, publicAssessment);
        await db.SaveChangesAsync();

        return (schoolStudent.Id, publicStudent.Id, schoolAssessment.Id, publicAssessment.Id);
    }

    private static Student MakeStudent(Guid schoolId, DateTimeOffset now, string fullName, string phone) =>
        Student.Create(Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Male, 9, PhoneNumber.Create(phone).Value, now, now);

    private static Assessment MakeAssessment(Guid studentId, Guid schoolId, DateTimeOffset now, string sessionToken, Guid programId) =>
        Assessment.Create(Guid.NewGuid(), studentId, schoolId, sessionToken, "uz", programId, startedAt: now, expiresAt: now.AddDays(7), now: now);

    [Fact]
    public async Task Students_SourceFiltri_IkkiOqimniAjratadi()
    {
        var (schoolStudentId, publicStudentId, _, _) = await SeedBothFlowsAsync("source-students", "+9989031110");

        using var client = await AuthenticatedClientAsync("source-students-admin");

        var schoolOnly = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?source=school&pageSize=100", TestJson.Options);
        schoolOnly!.Items.Should().Contain(s => s.Id == schoolStudentId);
        schoolOnly.Items.Should().NotContain(s => s.Id == publicStudentId);
        schoolOnly.Items.Should().OnlyContain(s => s.Source == "School");

        var publicOnly = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?source=public&pageSize=100", TestJson.Options);
        publicOnly!.Items.Should().Contain(s => s.Id == publicStudentId);
        publicOnly.Items.Should().NotContain(s => s.Id == schoolStudentId);
        publicOnly.Items.Should().OnlyContain(s => s.Source == "Public");

        // Filtrsiz — ikkalasi ham, LEKIN har qator o'z manbasini olib yuradi (ustun).
        var all = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?pageSize=100", TestJson.Options);
        all!.Items.Should().Contain(s => s.Id == schoolStudentId && s.Source == "School");
        all.Items.Should().Contain(s => s.Id == publicStudentId && s.Source == "Public");
    }

    [Fact]
    public async Task Students_NotoGriSourceQiymati_FiltrniOchiradi()
    {
        var (schoolStudentId, publicStudentId, _, _) = await SeedBothFlowsAsync("source-students-bad", "+9989031120");

        using var client = await AuthenticatedClientAsync("source-students-bad-admin");

        // Noto'g'ri qiymat JIMGINA bo'sh ro'yxat bermaydi ("hech narsa topilmadi" degan yolg'on
        // javob) — filtr shunchaki qo'llanmaydi (`AdminSourceFilter.Parse`).
        var result = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?source=maktab&pageSize=100", TestJson.Options);

        result!.Items.Should().Contain(s => s.Id == schoolStudentId);
        result.Items.Should().Contain(s => s.Id == publicStudentId);
    }

    [Fact]
    public async Task Assessments_SourceFiltri_IkkiOqimniAjratadi()
    {
        var (_, _, schoolAssessmentId, publicAssessmentId) = await SeedBothFlowsAsync("source-assess", "+9989031130");

        using var client = await AuthenticatedClientAsync("source-assessments-admin");

        var schoolOnly = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            "/api/admin/assessments?source=school&pageSize=100", TestJson.Options);
        schoolOnly!.Items.Should().Contain(a => a.Id == schoolAssessmentId);
        schoolOnly.Items.Should().NotContain(a => a.Id == publicAssessmentId);
        schoolOnly.Items.Should().OnlyContain(a => a.Source == "School");

        var publicOnly = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            "/api/admin/assessments?source=public&pageSize=100", TestJson.Options);
        publicOnly!.Items.Should().Contain(a => a.Id == publicAssessmentId);
        publicOnly.Items.Should().NotContain(a => a.Id == schoolAssessmentId);
        publicOnly.Items.Should().OnlyContain(a => a.Source == "Public");
    }

    [Fact]
    public async Task Dashboard_MaktabVaOmmaviyRaqamlari_Aralashmaydi()
    {
        await SeedBothFlowsAsync("source-dash", "+9989031140");

        using var client = await AuthenticatedClientAsync("source-dashboard-admin");

        // `from`/`to` — kesh kaliti testlar orasida to'qnashmasligi uchun ANIQ oyna
        // (`GetDashboardStatsQueryHandler.CacheKey` `source` ni ham hisobga oladi).
        var window = $"from={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"))}" +
            $"&to={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"))}";

        var schoolStats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?{window}&source=school", TestJson.Options);
        var publicStats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?{window}&source=public", TestJson.Options);

        schoolStats!.Source.Should().Be("School");
        publicStats!.Source.Should().Be("Public");

        // Maktab ko'lamida — maktablar soni bor; ommaviy ko'lamda `null` ("ma'lumot yo'q ≠ 0").
        schoolStats.Totals.Schools.Should().NotBeNull();
        schoolStats.Totals.ActiveSchools.Should().NotBeNull();
        publicStats.Totals.Schools.Should().BeNull("ommaviy makon maktab emas — bu son u yerda ma'nosiz");
        publicStats.Totals.ActiveSchools.Should().BeNull();

        // Har bir oqimda AYNAN o'zining foydalanuvchisi.
        publicStats.Totals.Students.Should().BeGreaterThanOrEqualTo(1);
        schoolStats.Totals.Students.Should().BeGreaterThanOrEqualTo(1);
        publicStats.SchoolBreakdown.Should().BeEmpty("ommaviy ko'lamda maktablar kesimi ma'nosiz");

        // Standart (parametrsiz) so'rov — MAKTAB ko'lami: panelning tarixiy ma'nosi saqlanadi
        // va ommaviy raqamlar uning ustiga jimgina qo'shilmaydi.
        var defaultStats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?{window}", TestJson.Options);
        defaultStats!.Source.Should().Be("School");
        defaultStats.Totals.Students.Should().Be(schoolStats.Totals.Students);
    }

    [Fact]
    public async Task Dashboard_TotalsSchools_OmmaviyMakonniSanamaydi()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, DateTimeOffset.UtcNow);
        }

        int expectedSchools;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            expectedSchools = db.Schools.Count(s => s.Kind == SchoolKind.School);
        }

        using var client = await AuthenticatedClientAsync("source-dashboard-totals-admin");

        var window = $"from={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-2).ToString("O"))}" +
            $"&to={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(2).ToString("O"))}";

        var stats = await client.GetFromJsonAsync<AdminDashboardStatsDto>(
            $"/api/admin/dashboard/stats?{window}", TestJson.Options);

        stats!.Totals.Schools.Should().Be(
            expectedSchools,
            "ommaviy makon 'jami maktablar' soniga qo'shilsa, admin hech qachon yaratmagan bitta ortiqcha maktabni ko'rardi");
    }
}

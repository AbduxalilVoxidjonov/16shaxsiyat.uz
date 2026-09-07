using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
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
/// **Maktab va ommaviy oqimlarni ajratish** (`AdminSourceFilter`). Boshqaruv paneli
/// (2026-09-06) `?source=` bilan ikki kesimli qoladi; o'quvchilar ro'yxati/eksporti va
/// sessiyalar ro'yxati esa (2026-09-07, egasining qarori) ommaviy makonni SHARTSIZ chiqarib
/// tashlaydi — `source` parametri va ustuni u yerlardan olib tashlangan.
///
/// <para>
/// Muammoning ildizi: ommaviy makon `schools` jadvalidagi qator bo'lgani uchun uning
/// foydalanuvchilari va sessiyalari admin ro'yxatlarida oddiy maktab qatorlari bilan
/// ARALASHIB ketardi — `schoolName` ustuni ikki oqimni ajratmasdi. Endi maktab bo'limlari
/// faqat maktabni ko'rsatadi, ommaviylar o'z bo'limida (`/admin/ommaviy`).
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

    /// <summary>
    /// O'quvchilar ro'yxati — FAQAT maktab (egasining talabi, 2026-09-07): ommaviy makon
    /// foydalanuvchisi `?source=` parametrisiz ham, `?source=public` bilan ham ro'yxatga
    /// TUSHMAYDI (parametr endi qabul qilinmaydi va jimgina e'tiborsiz qoldiriladi). Ular o'z
    /// bo'limida — `GET /api/admin/public-space/users` (`AdminPublicSpaceUsersEndpointTests`).
    /// </summary>
    [Fact]
    public async Task Students_Royxat_OmmaviyMakonOquvchisiniHechQachonQaytarmaydi()
    {
        var (schoolStudentId, publicStudentId, _, _) = await SeedBothFlowsAsync("source-students", "+9989031110");

        using var client = await AuthenticatedClientAsync("source-students-admin");

        var all = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?pageSize=100", TestJson.Options);
        all!.Items.Should().Contain(s => s.Id == schoolStudentId);
        all.Items.Should().NotContain(s => s.Id == publicStudentId, "ommaviy makon foydalanuvchilari o'quvchilar bo'limiga aralashmasligi kerak");

        // Eskirgan `?source=public` — parametr olib tashlangan, ommaviy o'quvchini "qaytarib" bermaydi.
        var legacyPublic = await client.GetFromJsonAsync<PagedResult<AdminStudentListItemDto>>(
            "/api/admin/students?source=public&pageSize=100", TestJson.Options);
        legacyPublic!.Items.Should().Contain(s => s.Id == schoolStudentId);
        legacyPublic.Items.Should().NotContain(s => s.Id == publicStudentId);
    }

    [Fact]
    public async Task Students_Eksport_OmmaviyMakonOquvchisiniChiqarmaydi()
    {
        var (_, _, _, _) = await SeedBothFlowsAsync("source-students-export", "+9989031120");

        using var client = await AuthenticatedClientAsync("source-students-export-admin");

        var response = await client.GetAsync(new Uri("/api/admin/students/export", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        var names = worksheet.Column(1).CellsUsed().Skip(1).Select(c => c.GetString()).ToList();

        names.Should().Contain("Maktab Oquvchisi source-students-export");
        names.Should().NotContain("Ommaviy Foydalanuvchi source-students-export", "eksport ro'yxat bilan AYNAN bir xil filtrni ishlatadi");
    }

    /// <summary>
    /// Sessiyalar ro'yxati — FAQAT maktab (egasining qarori, 2026-09-07): ommaviy makon
    /// sessiyasi parametrsiz ham, eskirgan `?source=public` bilan ham ro'yxatga TUSHMAYDI.
    /// `source` ustuni javobda yo'q (xom JSON ustidan — `ReadFromJsonAsync` ortiqcha kalitni
    /// ko'rmaydi). Ommaviy sessiya DETALI esa ochiq qoladi: admin unga ommaviy foydalanuvchi
    /// profilidan (`/admin/ommaviy` → `/admin/students/{id}`) boradi, va o'sha profil
    /// `{id}/answers`, `rerun-analysis`, `report.pdf` endpointlarini ham ishlatadi — ro'yxatdan
    /// yashirish ≠ yozuvni yo'q qilish.
    /// </summary>
    [Fact]
    public async Task Assessments_Royxat_OmmaviyMakonSessiyasiniHechQachonQaytarmaydi()
    {
        var (_, _, schoolAssessmentId, publicAssessmentId) = await SeedBothFlowsAsync("source-assess", "+9989031130");

        using var client = await AuthenticatedClientAsync("source-assessments-admin");

        var all = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            "/api/admin/assessments?pageSize=100", TestJson.Options);
        all!.Items.Should().Contain(a => a.Id == schoolAssessmentId);
        all.Items.Should().NotContain(a => a.Id == publicAssessmentId, "ommaviy makon sessiyalari sessiyalar bo'limiga aralashmasligi kerak");

        // Eskirgan `?source=public` — parametr olib tashlangan, ommaviy sessiyani "qaytarib" bermaydi.
        var legacyPublic = await client.GetFromJsonAsync<PagedResult<AdminAssessmentListItemDto>>(
            "/api/admin/assessments?source=public&pageSize=100", TestJson.Options);
        legacyPublic!.Items.Should().Contain(a => a.Id == schoolAssessmentId);
        legacyPublic.Items.Should().NotContain(a => a.Id == publicAssessmentId);

        // `source` ustuni javobdan olib tashlangan — xom JSON ustidan.
        var rawJson = await client.GetStringAsync(new Uri("/api/admin/assessments?pageSize=100", UriKind.Relative));
        rawJson.Should().NotContain("\"source\"", "ro'yxat faqat maktab sessiyalarini qaytargani uchun manba ustuni ma'nosiz");

        // Ommaviy sessiya detali ro'yxatdan yashirilgan bo'lsa ham OCHIQ (yuqoridagi izoh).
        var detail = await client.GetAsync(new Uri($"/api/admin/assessments/{publicAssessmentId}", UriKind.Relative));
        detail.StatusCode.Should().Be(HttpStatusCode.OK, "ommaviy foydalanuvchi profili shu sessiyaning detali/javoblariga tayanadi");
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

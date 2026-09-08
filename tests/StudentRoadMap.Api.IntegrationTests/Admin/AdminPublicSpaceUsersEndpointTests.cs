using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Admin.PublicSpace;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Identity.Login;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Admin;

/// <summary>
/// `GET /api/admin/public-space/users` (2026-09-07) — EF Core tarjimasi haqiqiy provayderda
/// (SQLite in-memory): `public_users ⟕ students` LEFT JOIN, oxirgi sessiya bo'yicha
/// korrelyatsiyalangan `ORDER BY … LIMIT 1` sub-so'rov (holat filtri), `UPPER(...) LIKE`
/// qidiruv, `OFFSET/LIMIT` sahifalash. Mantiqning o'zi `ListPublicSpaceUsersQueryHandlerTests`
/// da (DB'siz) qulflangan.
///
/// Fixture bitta bazani baham ko'radi — har test o'z `username` PREFIKSI bilan qidiradi.
///
/// ⚠️ `registeredAt` (`public_users.created_at`) bu yerda ORQAGA SURILMAYDI —
/// `AppDbContext.SaveChangesAsync` `Added` yozuvlarning `CreatedAt`ini o'zi `now` qiladi,
/// shu sabab standart `-registeredAt` tartibi bu testlarda barcha qatorlarda TENG bo'ladi
/// (tartib mantig'i `ListPublicSpaceUsersQueryHandlerTests`da qulflangan). DB darajasidagi
/// saralash `lastLoginAt` (`RecordLogin` — ustidan yozilmaydi) bilan tekshiriladi.
/// </summary>
public sealed class AdminPublicSpaceUsersEndpointTests : IClassFixture<PublicApiTestFactory>
{
    private readonly PublicApiTestFactory _factory;

    private static long _telegramSeed = 900_000_000;

    public AdminPublicSpaceUsersEndpointTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListUsers_Tokensiz_401Qaytaradi()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/admin/public-space/users", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListUsers_NomaLumHolat_400Qaytaradi()
    {
        using var client = await AuthenticatedClientAsync("public-space-users-400-admin");

        var response = await client.GetAsync(new Uri("/api/admin/public-space/users?status=hammasi", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListUsers_AnketasizJarayondagiVaTugallangan_QatorlarToGri()
    {
        const string prefix = "psu_a_";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var space = await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);

            // 1) Ro'yxatdan o'tgan, anketa to'ldirmagan.
            AddUser(db, $"{prefix}nobody", "Hech", "Kim", now.AddDays(-3));

            // 2) Jarayonda: 1-blok (2 savol) tugallangan, 2-blokda (44 savol) 17 javob, 3-blok boshlanmagan.
            var inProgressUser = AddUser(db, $"{prefix}progress", "Bobur", "Toshev", now.AddDays(-2));
            var inProgressStudent = AddStudent(db, space.Id, inProgressUser.Id, "Toshev Bobur", "+998905551001", now);
            await db.SaveChangesAsync();

            var block1 = await TestDataFactory.CreatePublishedTestAsync(db, now, "PSU-A-MBTI", 1, questionCount: 2);
            var block2 = await TestDataFactory.CreatePublishedTestAsync(db, now, "PSU-A-BIG5", 2, questionCount: 44);
            var block3 = await TestDataFactory.CreatePublishedTestAsync(db, now, "PSU-A-RIASEC", 3, questionCount: 2);
            var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);

            var inProgress = await BuildAssessmentAsync(
                db, inProgressStudent.Id, space.Id, programId, now.AddHours(-2), [block1, block2, block3],
                completedBlocks: 1, answeredOnCurrent: 17);
            db.Assessments.Add(inProgress);

            // 3) Tugallangan (ikkita sessiya — eskisi ham tugallangan).
            var completedUser = AddUser(db, $"{prefix}done", "Dilnoza", "Rahimova", now.AddDays(-1));
            var completedStudent = AddStudent(db, space.Id, completedUser.Id, "Rahimova Dilnoza", "+998905551002", now);
            await db.SaveChangesAsync();

            db.Assessments.Add(await BuildAssessmentAsync(
                db, completedStudent.Id, space.Id, programId, now.AddDays(-20), [block1, block3], completedBlocks: 2, answeredOnCurrent: 0, complete: true));
            db.Assessments.Add(await BuildAssessmentAsync(
                db, completedStudent.Id, space.Id, programId, now.AddHours(-1), [block1, block3], completedBlocks: 2, answeredOnCurrent: 0, complete: true));
            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientAsync("public-space-users-list-admin");

        var page = await client.GetFromJsonAsync<PagedResult<AdminPublicUserListItemDto>>(
            $"/api/admin/public-space/users?search={prefix}", TestJson.Options);

        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(3);
        page.Items.Select(i => i.Telegram.Username).Should().BeEquivalentTo([$"{prefix}done", $"{prefix}progress", $"{prefix}nobody"]);

        var nobody = page.Items.Single(i => i.Telegram.Username == $"{prefix}nobody");
        nobody.StudentId.Should().BeNull();
        nobody.FullName.Should().BeNull();
        nobody.Phone.Should().BeNull("anketa yo'q — telefon ham yo'q");
        nobody.Assessments.Total.Should().Be(0);
        nobody.LastAssessment.Should().BeNull();

        var progress = page.Items.Single(i => i.Telegram.Username == $"{prefix}progress");
        progress.FullName.Should().Be("Toshev Bobur");
        progress.Phone.Should().Be("+998905551001");
        progress.StudentId.Should().NotBeNull();
        progress.Assessments.Should().Be(new AdminPublicUserAssessmentCountsDto(1, 0, 1));
        progress.LastAssessment.Should().NotBeNull();
        progress.LastAssessment!.Status.Should().Be(nameof(AssessmentStatus.InProgress));
        progress.LastAssessment.Progress.Should().NotBeNull();
        progress.LastAssessment.Progress!.TestsTotal.Should().Be(3);
        progress.LastAssessment.Progress.TestsCompleted.Should().Be(1);
        progress.LastAssessment.Progress.CurrentTestNumber.Should().Be(2);
        progress.LastAssessment.Progress.CurrentTestCode.Should().Be("PSU-A-BIG5");
        progress.LastAssessment.Progress.CurrentTestName.Should().Be("PSU-A-BIG5 nomi");
        progress.LastAssessment.Progress.Answered.Should().Be(17);
        progress.LastAssessment.Progress.QuestionsTotal.Should().Be(44);

        var done = page.Items.Single(i => i.Telegram.Username == $"{prefix}done");
        done.Assessments.Should().Be(new AdminPublicUserAssessmentCountsDto(2, 2, 0));
        done.LastAssessment!.Status.Should().Be(nameof(AssessmentStatus.Completed));
        done.LastAssessment.CompletedAt.Should().NotBeNull();
        done.LastAssessment.Progress.Should().BeNull();

        // Holat filtri — DB darajasidagi korrelyatsiyalangan sub-so'rov.
        (await GetUsernamesAsync(client, $"search={prefix}&status=never_started")).Should().Equal($"{prefix}nobody");
        (await GetUsernamesAsync(client, $"search={prefix}&status=in_progress")).Should().Equal($"{prefix}progress");
        (await GetUsernamesAsync(client, $"search={prefix}&status=completed")).Should().Equal($"{prefix}done");
    }

    [Fact]
    public async Task ListUsers_ToLiqTelefonRaqamiBoYichaTopadi_QismanRaqamHechNarsaTopmaydi()
    {
        const string prefix = "psu_d_";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var space = await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);

            var user = AddUser(db, $"{prefix}phone", "Nodir", "Egamberdiyev", now);
            AddStudent(db, space.Id, user.Id, "Egamberdiyev Nodir", "+998907778899", now);
            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientAsync("public-space-users-phone-admin");

        // Xalqaro shakl (+998 bilan, 12 xona) — EF value-converter'li ustunga TENGLIK
        // sifatida ishonchli tarjima qilinishi shu testda tekshiriladi (SQLite provayder).
        (await GetUsernamesAsync(client, "search=%2B998907778899")).Should().Equal($"{prefix}phone");

        // Mahalliy shakl (+998 siz, 9 xona).
        (await GetUsernamesAsync(client, "search=907778899")).Should().Equal($"{prefix}phone");

        // Qisman raqam — moslik YO'Q (faqat to'liq tenglik, `Contains` emas).
        (await GetUsernamesAsync(client, "search=90777")).Should().BeEmpty();
    }

    [Fact]
    public async Task ListUsers_QidiruvVaSahifalash_DBDarajasida()
    {
        const string prefix = "psu_b_";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var space = await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);

            for (var i = 0; i < 5; i++)
            {
                var user = AddUser(db, $"{prefix}u{i}", $"Ism{i}", "Familiya", now.AddDays(-10));
                user.RecordLogin(now.AddDays(-i), username: $"{prefix}u{i}", firstName: $"Ism{i}", lastName: "Familiya");
                if (i == 2)
                {
                    AddStudent(db, space.Id, user.Id, "Qidiruv Nishoni Anketasi", "+998905552002", now);
                }
            }

            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientAsync("public-space-users-search-admin");

        // Sahifalash DB darajasida: 5 ta yozuv, 2 talik sahifalar — jami 3 sahifa, har biri
        // takrorlanmaydigan, birlashmasi to'liq to'plam (tartib `lastLoginAt` bo'yicha
        // deterministik — sinf izohiga qarang).
        var seen = new List<string?>();
        for (var pageNumber = 1; pageNumber <= 3; pageNumber++)
        {
            var page = await client.GetFromJsonAsync<PagedResult<AdminPublicUserListItemDto>>(
                $"/api/admin/public-space/users?search={prefix}&sort=-lastLoginAt&page={pageNumber}&pageSize=2", TestJson.Options);
            page!.TotalCount.Should().Be(5);
            page.TotalPages.Should().Be(3);
            page.Items.Should().HaveCount(pageNumber == 3 ? 1 : 2);
            seen.AddRange(page.Items.Select(i => i.Telegram.Username));
        }

        // `-lastLoginAt`: u0 eng yaqinda kirgan (`now`), u4 eng oldin (`now − 4 kun`).
        seen.Should().Equal($"{prefix}u0", $"{prefix}u1", $"{prefix}u2", $"{prefix}u3", $"{prefix}u4");

        // Katta-kichik harf farqsiz — Telegram ism va anketa F.I.Sh. bo'yicha.
        (await GetUsernamesAsync(client, "search=ISM3")).Should().Contain($"{prefix}u3");
        (await GetUsernamesAsync(client, "search=nishoni")).Should().Equal($"{prefix}u2");

        // Saralash oq ro'yxati: `lastLoginAt` o'sish — eng oldin kirgani birinchi.
        (await GetUsernamesAsync(client, $"search={prefix}&sort=lastLoginAt&pageSize=100"))
            .Should().Equal($"{prefix}u4", $"{prefix}u3", $"{prefix}u2", $"{prefix}u1", $"{prefix}u0");
    }

    [Fact]
    public async Task ListUsers_OChirilganAkkaunt_KoRinmaydiLekinStatistikadaSanaladi()
    {
        const string prefix = "psu_c_";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            await PublicUserTestDataFactory.GetOrCreatePublicSpaceAsync(db, now);

            AddUser(db, $"{prefix}alive", "Tirik", null, now);
            var deleted = AddUser(db, $"{prefix}deleted", "O'chirilgan", null, now);
            deleted.MarkDeleted(now);
            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientAsync("public-space-users-deleted-admin");

        (await GetUsernamesAsync(client, $"search={prefix}")).Should().Equal($"{prefix}alive");

        var space = await client.GetFromJsonAsync<AdminPublicSpaceDto>("/api/admin/public-space", TestJson.Options);
        space!.Stats.DeletedUserCount.Should().BeGreaterThanOrEqualTo(1, "anonimlashtirilgan akkauntlar faqat SON sifatida ko'rinadi");
        space.Stats.UserCount.Should().BeGreaterThanOrEqualTo(1);
    }

    // ————— Yordamchilar —————

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

    private static async Task<List<string?>> GetUsernamesAsync(HttpClient client, string query)
    {
        var page = await client.GetFromJsonAsync<PagedResult<AdminPublicUserListItemDto>>(
            $"/api/admin/public-space/users?{query}", TestJson.Options);
        return page!.Items.Select(i => i.Telegram.Username).ToList();
    }

    private static PublicUser AddUser(AppDbContext db, string username, string? firstName, string? lastName, DateTimeOffset registeredAt)
    {
        var user = PublicUser.Create(Guid.NewGuid(), Interlocked.Increment(ref _telegramSeed), registeredAt, username, firstName, lastName);
        db.PublicUsers.Add(user);
        return user;
    }

    private static Student AddStudent(AppDbContext db, Guid spaceId, Guid publicUserId, string fullName, string phone, DateTimeOffset now)
    {
        var student = Student.Create(
            Guid.NewGuid(), spaceId, fullName, new DateOnly(2008, 5, 5), Gender.Male, 10,
            PhoneNumber.Create(phone).Value, now, now, publicUserId: publicUserId);
        db.Students.Add(student);
        return student;
    }

    /// <summary>
    /// Birinchi `completedBlocks` blok yakunlangan, keyingisida `answeredOnCurrent` HAQIQIY savol
    /// ID'lari bilan javob (`answers.question_id` FK). `complete` — sessiya yopiladi.
    /// </summary>
    private static async Task<Assessment> BuildAssessmentAsync(
        AppDbContext db,
        Guid studentId,
        Guid spaceId,
        Guid programId,
        DateTimeOffset startedAt,
        IReadOnlyList<TestDefinition> blocks,
        int completedBlocks,
        int answeredOnCurrent,
        bool complete = false)
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(), studentId, spaceId, $"psu-token-{Guid.NewGuid():N}", "uz", programId,
            startedAt, startedAt.AddDays(7), startedAt);

        var tests = new List<AssessmentTest>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var questionCount = await db.Questions.AsNoTracking().CountAsync(q => q.TestDefinitionId == blocks[i].Id);
            var test = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, blocks[i].Id, i + 1, questionCount);
            assessment.AddTest(test);
            tests.Add(test);
        }

        for (var i = 0; i < completedBlocks; i++)
        {
            var questionIds = await db.Questions.AsNoTracking()
                .Where(q => q.TestDefinitionId == blocks[i].Id)
                .OrderBy(q => q.DisplayOrder)
                .Select(q => q.Id)
                .ToListAsync();

            assessment.StartTest(blocks[i].Id, startedAt);
            foreach (var questionId in questionIds)
            {
                tests[i].UpsertAnswer(Guid.NewGuid(), questionId, 3, null, 1000, startedAt);
            }

            assessment.CompleteTest(blocks[i].Id, questionIds, startedAt);
        }

        if (completedBlocks < blocks.Count && answeredOnCurrent > 0)
        {
            var questionIds = await db.Questions.AsNoTracking()
                .Where(q => q.TestDefinitionId == blocks[completedBlocks].Id)
                .OrderBy(q => q.DisplayOrder)
                .Select(q => q.Id)
                .Take(answeredOnCurrent)
                .ToListAsync();

            assessment.StartTest(blocks[completedBlocks].Id, startedAt);
            foreach (var questionId in questionIds)
            {
                tests[completedBlocks].UpsertAnswer(Guid.NewGuid(), questionId, 3, null, 1000, startedAt);
            }
        }

        if (complete)
        {
            assessment.Complete(startedAt.AddMinutes(20));
        }

        return assessment;
    }
}

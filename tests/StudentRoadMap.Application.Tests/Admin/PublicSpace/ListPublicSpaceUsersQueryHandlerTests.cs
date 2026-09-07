using FluentAssertions;
using StudentRoadMap.Application.Admin.PublicSpace;
using StudentRoadMap.Application.Admin.PublicSpace.ListUsers;
using StudentRoadMap.Application.Tests.Admin.PublicSpace.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Admin.PublicSpace;

/// <summary>
/// `ListPublicSpaceUsersQueryHandler` — ommaviy makon foydalanuvchilari ro'yxati (2026-09-07).
/// DB'siz sinov (`GetSchoolByIdQueryHandlerTests` naqshi); EF tarjimasi (LEFT JOIN, korrelyatsiyalangan
/// sub-so'rov, `UPPER LIKE`) integratsiya testida (`AdminPublicSpaceUsersEndpointTests`).
///
/// Bu yerda qulflanadigan asosiy shartlar: (1) hali anketa to'ldirmagan foydalanuvchi ham
/// ko'rinadi; (2) "qayerda to'xtagan" — ommaviy `GetSessionState` bilan bir xil qoida
/// (`SessionProgressCalculator`); (3) so'rovlar soni sahifa hajmiga bog'liq emas (N+1 yo'q).
/// </summary>
public sealed class ListPublicSpaceUsersQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SpaceId = Guid.NewGuid();
    private static readonly Guid ProgramId = Guid.NewGuid();

    private readonly FakePublicSpaceAppDbContext _context = new();
    private readonly CountingInlineAsyncQueryExecutor _executor = new();

    private int _telegramSeed = 1000;
    private int _phoneSeed = 1000;

    [Fact]
    public async Task Handle_AnketaToLdirmaganFoydalanuvchi_RoYxatdaKoRinadi()
    {
        var user = AddUser("Ali", "Valiyev", "ali_v", registeredAt: Now.AddDays(-2));

        var page = await HandleAsync();

        page.TotalCount.Should().Be(1);
        var item = page.Items.Single();
        item.PublicUserId.Should().Be(user.Id);
        item.Telegram.FirstName.Should().Be("Ali");
        item.Telegram.Username.Should().Be("ali_v");
        item.RegisteredAt.Should().Be(Now.AddDays(-2));
        item.StudentId.Should().BeNull("anketa hali to'ldirilmagan");
        item.FullName.Should().BeNull();
        item.Age.Should().BeNull();
        item.Grade.Should().BeNull();
        item.Assessments.Should().Be(new AdminPublicUserAssessmentCountsDto(0, 0, 0));
        item.LastAssessment.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AnketaBorLekinSessiyaYoq_ProfilBilanKoRinadi()
    {
        var user = AddUser("Zulfiya", null, null);
        var student = AddStudent(user, "Karimova Zulfiya", grade: 9, birthDate: new DateOnly(2009, 3, 1));

        var item = (await HandleAsync()).Items.Single();

        item.StudentId.Should().Be(student.Id);
        item.FullName.Should().Be("Karimova Zulfiya");
        item.Age.Should().Be(17);
        item.Grade.Should().Be(9);
        item.LastAssessment.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SinfYoq_GradeNullQaytaradi()
    {
        var user = AddUser("Katta", "Odam", "katta");
        AddStudent(user, "Katta Odam", grade: Student.NoGrade, birthDate: new DateOnly(1990, 1, 1));

        var item = (await HandleAsync()).Items.Single();

        item.Grade.Should().BeNull("`NoGrade` (0) — 'sinf yo'q', UI'da 0-sinf ko'rinmasin");
        item.Age.Should().Be(36);
    }

    [Fact]
    public async Task Handle_JarayondagiSessiya_QayerdaToXtaganiniHisoblaydi()
    {
        var user = AddUser("Bobur", "Toshev", "bobur");
        var student = AddStudent(user, "Toshev Bobur");
        var definitions = AddDefinitions("MBTI16", "BIG5", "RIASEC", "ACTIVITY");

        // 4 blok: 1-si tugallangan, 2-sida (BIG5, 44 savol) 17 javob, qolganlari boshlanmagan.
        AddAssessment(student, Now.AddHours(-3), definitions, completedBlocks: 1, answeredOnCurrent: 17, questionsPerBlock: 44);

        var item = (await HandleAsync()).Items.Single();

        item.Assessments.Should().Be(new AdminPublicUserAssessmentCountsDto(Total: 1, Completed: 0, InProgress: 1));
        item.LastAssessment.Should().NotBeNull();
        item.LastAssessment!.Status.Should().Be(nameof(AssessmentStatus.InProgress));
        item.LastAssessment.CompletedAt.Should().BeNull();

        var progress = item.LastAssessment.Progress;
        progress.Should().NotBeNull("yakunlanmagan sessiyada progress bo'lishi shart");
        progress!.TestsTotal.Should().Be(4);
        progress.TestsCompleted.Should().Be(1);
        progress.CurrentTestNumber.Should().Be(2, "4 dan 2-blok");
        progress.CurrentTestCode.Should().Be("BIG5");
        progress.CurrentTestName.Should().Be("BIG5 nomi");
        progress.Answered.Should().Be(17);
        progress.QuestionsTotal.Should().Be(44);
    }

    [Fact]
    public async Task Handle_TugallanganSessiya_ProgressNull()
    {
        var user = AddUser("Dilnoza", "Rahimova", "dilnoza");
        var student = AddStudent(user, "Rahimova Dilnoza");
        var definitions = AddDefinitions("MBTI16", "BIG5");

        AddAssessment(student, Now.AddDays(-1), definitions, completedBlocks: 2, answeredOnCurrent: 0, questionsPerBlock: 10, complete: true);

        var item = (await HandleAsync()).Items.Single();

        item.Assessments.Should().Be(new AdminPublicUserAssessmentCountsDto(Total: 1, Completed: 1, InProgress: 0));
        item.LastAssessment!.Status.Should().Be(nameof(AssessmentStatus.Completed));
        item.LastAssessment.CompletedAt.Should().NotBeNull();
        item.LastAssessment.Progress.Should().BeNull("tugallangan sessiyada 'qayerda to'xtagan' ma'nosiz");
    }

    [Fact]
    public async Task Handle_BirNechaSessiya_OxirgisiVaHisoblarToGri()
    {
        var user = AddUser("Sardor", "Aliyev", "sardor");
        var student = AddStudent(user, "Aliyev Sardor");
        var definitions = AddDefinitions("MBTI16", "BIG5");

        AddAssessment(student, Now.AddDays(-30), definitions, completedBlocks: 2, answeredOnCurrent: 0, questionsPerBlock: 10, complete: true);
        AddAssessment(student, Now.AddDays(-10), definitions, completedBlocks: 2, answeredOnCurrent: 0, questionsPerBlock: 10, complete: true);
        var latest = AddAssessment(student, Now.AddHours(-1), definitions, completedBlocks: 0, answeredOnCurrent: 3, questionsPerBlock: 10);

        var item = (await HandleAsync()).Items.Single();

        item.Assessments.Should().Be(new AdminPublicUserAssessmentCountsDto(Total: 3, Completed: 2, InProgress: 1));
        item.LastAssessment!.Id.Should().Be(latest.Id, "`StartedAt` bo'yicha eng yangisi");
        item.LastAssessment.Progress!.CurrentTestNumber.Should().Be(1);
        item.LastAssessment.Progress.Answered.Should().Be(3);
    }

    [Fact]
    public async Task Handle_TashlabKetilganSessiya_ProgressBorVaInProgressFiltrigaKiradi()
    {
        var user = AddUser("Jasur", null, "jasur");
        var student = AddStudent(user, "Jasur Xolmatov");
        var definitions = AddDefinitions("MBTI16", "BIG5");
        var assessment = AddAssessment(student, Now.AddDays(-20), definitions, completedBlocks: 1, answeredOnCurrent: 2, questionsPerBlock: 10);
        assessment.MarkAbandoned(Now.AddDays(-10));

        var all = await HandleAsync();
        var item = all.Items.Single();
        item.LastAssessment!.Status.Should().Be(nameof(AssessmentStatus.Abandoned));
        item.LastAssessment.Progress.Should().NotBeNull("tashlab ketilgan bo'lsa ham QAYERDA to'xtagani ko'rinsin");
        item.Assessments.InProgress.Should().Be(0, "`Abandoned` jarayonda EMAS");

        var inProgress = await HandleAsync(status: PublicUserStatusFilter.InProgress);
        inProgress.TotalCount.Should().Be(1, "`in_progress` = oxirgi sessiya yakunlanmagan (`CompletedAt == null`)");
    }

    [Fact]
    public async Task Handle_HolatFiltri_OxirgiSessiyaBoYicha()
    {
        var definitions = AddDefinitions("MBTI16", "BIG5");

        AddUser("Hech", "Kim", "nobody"); // anketa yo'q
        var registeredNoSession = AddStudent(AddUser("Anketa", "Bor", "anketa"), "Anketa Bor");
        _ = registeredNoSession;
        var inProgressStudent = AddStudent(AddUser("Jarayon", "Da", "jarayon"), "Jarayon Da");
        AddAssessment(inProgressStudent, Now.AddHours(-2), definitions, completedBlocks: 0, answeredOnCurrent: 1, questionsPerBlock: 10);
        var completedStudent = AddStudent(AddUser("Tugal", "Langan", "tugal"), "Tugal Langan");
        AddAssessment(completedStudent, Now.AddHours(-5), definitions, completedBlocks: 2, answeredOnCurrent: 0, questionsPerBlock: 10, complete: true);

        (await HandleAsync(status: PublicUserStatusFilter.All)).TotalCount.Should().Be(4);
        (await HandleAsync(status: PublicUserStatusFilter.NeverStarted)).Items
            .Select(i => i.Telegram.Username).Should().BeEquivalentTo(["nobody", "anketa"]);
        (await HandleAsync(status: PublicUserStatusFilter.InProgress)).Items
            .Select(i => i.Telegram.Username).Should().BeEquivalentTo(["jarayon"]);
        (await HandleAsync(status: PublicUserStatusFilter.Completed)).Items
            .Select(i => i.Telegram.Username).Should().BeEquivalentTo(["tugal"]);
    }

    [Fact]
    public async Task Handle_Qidiruv_TelegramVaFishBoYichaKattaKichikHarfFarqsiz()
    {
        AddUser("Ali", "Valiyev", "ali_v");
        AddStudent(AddUser("Tg", "Ism", "boshqa"), "Karimova Zulfiya");
        AddUser("Uchinchi", "Odam", "uchinchi");

        (await HandleAsync(search: "ali")).Items.Should().ContainSingle(i => i.Telegram.Username == "ali_v");
        (await HandleAsync(search: "VALIYEV")).Items.Should().ContainSingle(i => i.Telegram.Username == "ali_v");
        (await HandleAsync(search: "boshqa")).Items.Should().ContainSingle(i => i.Telegram.Username == "boshqa");
        (await HandleAsync(search: "zulfiya")).Items.Should().ContainSingle(i => i.FullName == "Karimova Zulfiya");
        (await HandleAsync(search: "yo'q odam")).TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Sahifalash_StandartTartibRoYxatdanOTganVaqtKamayish()
    {
        for (var i = 0; i < 5; i++)
        {
            AddUser($"U{i}", null, $"u{i}", registeredAt: Now.AddDays(-i));
        }

        var first = await HandleAsync(page: 1, pageSize: 2);
        first.TotalCount.Should().Be(5);
        first.TotalPages.Should().Be(3);
        first.Items.Select(i => i.Telegram.Username).Should().Equal("u0", "u1");

        var last = await HandleAsync(page: 3, pageSize: 2);
        last.Items.Select(i => i.Telegram.Username).Should().Equal("u4");

        var oldestFirst = await HandleAsync(page: 1, pageSize: 5, sort: "registeredAt");
        oldestFirst.Items.Select(i => i.Telegram.Username).Should().Equal("u4", "u3", "u2", "u1", "u0");
    }

    [Fact]
    public async Task Handle_OxirgiKirishBoYichaSaralash()
    {
        var a = AddUser("A", null, "a", registeredAt: Now.AddDays(-5));
        var b = AddUser("B", null, "b", registeredAt: Now.AddDays(-4));
        a.RecordLogin(Now.AddHours(-1), firstName: "A", username: "a");
        b.RecordLogin(Now.AddDays(-3), firstName: "B", username: "b");

        (await HandleAsync(sort: "-lastLoginAt")).Items.Select(i => i.Telegram.Username).Should().Equal("a", "b");
        (await HandleAsync(sort: "lastLoginAt")).Items.Select(i => i.Telegram.Username).Should().Equal("b", "a");
    }

    [Fact]
    public async Task Handle_OChirilganAkkaunt_RoYxatdaKoRinmaydi()
    {
        var deleted = AddUser("O'chirilgan", "Odam", "deleted");
        AddStudent(deleted, "O'chirilgan Odam");
        deleted.MarkDeleted(Now.AddDays(-1));
        AddUser("Tirik", "Odam", "alive");

        var page = await HandleAsync();

        page.TotalCount.Should().Be(1);
        page.Items.Single().Telegram.Username.Should().Be("alive");
    }

    [Fact]
    public async Task Handle_SoRovlarSoni_SahifaHajmigaBoGliqEmas()
    {
        var definitions = AddDefinitions("MBTI16", "BIG5", "RIASEC", "ACTIVITY");

        // 1 foydalanuvchi (jarayonda) → so'rovlar soni.
        var single = AddStudent(AddUser("Yakka", null, "yakka"), "Yakka Odam");
        AddAssessment(single, Now.AddHours(-1), definitions, completedBlocks: 1, answeredOnCurrent: 5, questionsPerBlock: 20);

        await HandleAsync(pageSize: 100);
        var withOne = _executor.QueryCount;

        // +30 foydalanuvchi, har birida jarayondagi va tugallangan sessiyalar.
        for (var i = 0; i < 30; i++)
        {
            var student = AddStudent(AddUser($"K{i}", null, $"k{i}"), $"K{i} Odam");
            AddAssessment(student, Now.AddDays(-2 - i), definitions, completedBlocks: 4, answeredOnCurrent: 0, questionsPerBlock: 20, complete: true);
            AddAssessment(student, Now.AddHours(-i - 2), definitions, completedBlocks: i % 4, answeredOnCurrent: i % 20, questionsPerBlock: 20);
        }

        var before = _executor.QueryCount;
        var page = await HandleAsync(pageSize: 100);
        var withMany = _executor.QueryCount - before;

        page.TotalCount.Should().Be(31);
        withMany.Should().Be(withOne, "N+1 yo'q: so'rovlar soni foydalanuvchi soniga bog'liq bo'lmasin");
        withMany.Should().BeLessThanOrEqualTo(5, "handler izohi: jami 5 so'rov");
    }

    [Fact]
    public async Task Handle_BoShSahifa_QoShimchaSoRovYubormaydi()
    {
        var page = await HandleAsync();

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
        _executor.QueryCount.Should().Be(2, "COUNT + sahifa; bo'sh sahifada sessiya so'rovlari yuborilmaydi");
    }

    // ————— Yordamchilar —————

    private async Task<Application.Common.Models.PagedResult<AdminPublicUserListItemDto>> HandleAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        string? sort = null)
    {
        var handler = new ListPublicSpaceUsersQueryHandler(_context, _executor, new FixedPublicSpaceDateTime(Now));
        var result = await handler.Handle(new ListPublicSpaceUsersQuery(search, status, page, pageSize, sort), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    private PublicUser AddUser(string? firstName, string? lastName, string? username, DateTimeOffset? registeredAt = null)
    {
        var user = PublicUser.Create(Guid.NewGuid(), _telegramSeed++, registeredAt ?? Now.AddDays(-1), username, firstName, lastName);
        _context.PublicUserList.Add(user);
        return user;
    }

    private Student AddStudent(PublicUser user, string fullName, int grade = 10, DateOnly? birthDate = null)
    {
        var student = Student.Create(
            Guid.NewGuid(),
            SpaceId,
            fullName,
            birthDate ?? new DateOnly(2008, 1, 1),
            Gender.Male,
            grade,
            PhoneNumber.Create($"+99890{_phoneSeed++:0000000}").Value,
            Now,
            Now,
            publicUserId: user.Id);
        _context.StudentList.Add(student);
        return student;
    }

    private List<TestDefinition> AddDefinitions(params string[] codes)
    {
        var list = new List<TestDefinition>();
        for (var i = 0; i < codes.Length; i++)
        {
            var definition = TestDefinition.Create(Guid.NewGuid(), codes[i], $"{codes[i]} nomi", i + 1, estimatedMinutes: 5, scoringStrategyCode: "SUM", now: Now);
            _context.TestDefinitionList.Add(definition);
            list.Add(definition);
        }

        return list;
    }

    /// <summary>
    /// Sessiya: birinchi `completedBlocks` blok yakunlangan, keyingisida `answeredOnCurrent`
    /// javob (0 bo'lsa ham `completedBlocks &lt; blocks` bo'lsa blok boshlangan hisoblanadi — `Draft`
    /// dan chiqish uchun), `complete` — butun sessiya yopiladi (`Completed`).
    /// </summary>
    private Assessment AddAssessment(
        Student student,
        DateTimeOffset startedAt,
        IReadOnlyList<TestDefinition> definitions,
        int completedBlocks,
        int answeredOnCurrent,
        int questionsPerBlock,
        bool complete = false)
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(), student.Id, SpaceId, $"token-{Guid.NewGuid():N}", "uz", ProgramId,
            startedAt, startedAt.AddDays(7), startedAt);

        var tests = new List<AssessmentTest>();
        for (var i = 0; i < definitions.Count; i++)
        {
            var test = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, definitions[i].Id, i + 1, questionsPerBlock);
            assessment.AddTest(test);
            tests.Add(test);
        }

        for (var i = 0; i < completedBlocks && i < definitions.Count; i++)
        {
            assessment.StartTest(definitions[i].Id, startedAt);
            assessment.CompleteTest(definitions[i].Id, [], startedAt);
        }

        if (completedBlocks < definitions.Count && (answeredOnCurrent > 0 || !complete))
        {
            assessment.StartTest(definitions[completedBlocks].Id, startedAt);
            for (var i = 0; i < answeredOnCurrent; i++)
            {
                tests[completedBlocks].UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, startedAt);
            }
        }

        if (complete)
        {
            assessment.Complete(startedAt.AddMinutes(30));
        }

        _context.AssessmentList.Add(assessment);
        return assessment;
    }
}

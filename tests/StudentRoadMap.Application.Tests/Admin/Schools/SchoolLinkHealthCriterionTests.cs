using FluentAssertions;
using Microsoft.Extensions.Logging;
using StudentRoadMap.Application.Admin.Schools.List;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.GetSchoolInfo;
using StudentRoadMap.Application.Tests.Admin.Schools.Testing;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Tests.Admin.Schools;

/// <summary>
/// **BU FAYL — ENG MUHIM QULF** (2026-09-03 jonli hodisasi).
///
/// Jonli bazada yagona dastur `is_active = false` + `Visibility = Assigned` (0 ta maktab)
/// qilib qo'yilgan edi: o'quvchi havolasi ishlamasdi, LEKIN admin panelida hech qanday belgi
/// yo'q edi. Endi panelda belgi bor — va u YOLG'ON aytmasligi uchun uning mezoni ommaviy
/// handler mezoni bilan AYNAN bir xil bo'lishi shart.
///
/// Shu sabab bu yerda IKKITA qulf bor:
///
/// 1. <see cref="MezonningIkkiShakli_HarQandayKombinatsiyadaBirXilJavobBeradi"/> —
///    `ProgramAvailability.Filter` (EF/DB) va `ProgramAvailability.Matches` (xotira, admin
///    batch hisobi) to'liq dekart ko'paytmasida bir xil.
/// 2. <see cref="PanelBelgisi_OmmaviyJavobBilanHechQachonAjralmaydi"/> — HAQIQIY
///    `GetSchoolInfoQueryHandler` va HAQIQIY `ListSchoolsQueryHandler` bir xil ma'lumot ustida
///    ishlatiladi va ularning xulosasi bir xilligi tekshiriladi. Mezonlar ajralsa (masalan
///    kimdir faqat bittasiga shart qo'shsa) — bu test yiqiladi.
/// </summary>
public sealed class SchoolLinkHealthCriterionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MezonningIkkiShakli_HarQandayKombinatsiyadaBirXilJavobBeradi()
    {
        var checkedCombinations = 0;

        foreach (var status in Enum.GetValues<ProgramStatus>())
        {
            foreach (var isActive in new[] { true, false })
            {
                foreach (var visibility in Enum.GetValues<ProgramVisibility>())
                {
                    foreach (var assigned in new[] { true, false })
                    {
                        var program = BuildProgram(status, isActive, visibility);
                        var assignedIds = assigned ? new List<Guid> { program.Id } : [];

                        var viaExpression = ProgramAvailability.Filter(assignedIds).Compile()(program);
                        var viaMemory = ProgramAvailability.Matches(program, assigned);

                        viaMemory.Should().Be(
                            viaExpression,
                            "mezon IKKI shaklda yozilgan (DB `Where` va xotiradagi admin hisobi) — " +
                            "ular ajralsa panel \"hammasi joyida\" deb yolg'on aytadi. " +
                            "Status={0}, IsActive={1}, Visibility={2}, assigned={3}",
                            status,
                            isActive,
                            visibility,
                            assigned);

                        checkedCombinations++;
                    }
                }
            }
        }

        // Ro'yxatga yangi `ProgramStatus`/`ProgramVisibility` qiymati qo'shilsa bu son o'sadi —
        // test o'z-o'zidan yangi qiymatni ham qamrab oladi (qattiq ro'yxat yo'q).
        checkedCombinations.Should().Be(
            Enum.GetValues<ProgramStatus>().Length * 2 * Enum.GetValues<ProgramVisibility>().Length * 2);
    }

    /// <summary>
    /// Har bir stsenariy uchun: ommaviy `GET /api/public/schools/{slug}` handleri MUVAFFAQIYAT
    /// qaytaradimi ⟺ admin ro'yxatidagi `linkHealth.availableProgramCount > 0`.
    /// </summary>
    [Theory]
    [InlineData(ProgramStatus.Published, true, ProgramVisibility.Public, false, true)]
    [InlineData(ProgramStatus.Published, true, ProgramVisibility.Assigned, true, true)]
    // Jonli hodisaning AYNAN o'zi: o'chirilgan + `Assigned` + biriktirilmagan.
    [InlineData(ProgramStatus.Published, false, ProgramVisibility.Assigned, false, false)]
    [InlineData(ProgramStatus.Published, false, ProgramVisibility.Public, false, false)]
    [InlineData(ProgramStatus.Published, true, ProgramVisibility.Assigned, false, false)]
    [InlineData(ProgramStatus.Draft, true, ProgramVisibility.Public, false, false)]
    [InlineData(ProgramStatus.Archived, true, ProgramVisibility.Public, false, false)]
    [InlineData(ProgramStatus.Archived, true, ProgramVisibility.Assigned, true, false)]
    public async Task PanelBelgisi_OmmaviyJavobBilanHechQachonAjralmaydi(
        ProgramStatus status,
        bool isActive,
        ProgramVisibility visibility,
        bool assignedToSchool,
        bool kutilganMavjudlik)
    {
        const string token = "kriteriya-qulf-tokeni-0123456789abcdefghij";
        var slug = SchoolSlug.Create("kriteriya-qulf-maktabi").Value;
        var school = School.Create(Guid.NewGuid(), "Kriteriya Qulf Maktabi", "Toshkent", "Yunusobod", slug, token, Now);

        var context = new FakeSchoolsAppDbContext();
        var executor = new SchoolsInlineAsyncQueryExecutor();
        context.SchoolList.Add(school);

        var (testDefinition, question) = BuildUsableTest();
        context.TestDefinitionList.Add(testDefinition);
        context.QuestionList.Add(question);

        var program = BuildProgram(status, isActive, visibility);
        context.ProgramList.Add(program);
        context.ProgramTestList.Add(ProgramTest.Create(Guid.NewGuid(), program.Id, testDefinition.Id, 1));

        if (assignedToSchool)
        {
            context.SchoolProgramList.Add(SchoolProgram.Create(Guid.NewGuid(), school.Id, program.Id, Now));
        }

        // ————— OMMAVIY javob —————
        var publicHandler = new GetSchoolInfoQueryHandler(
            context, executor, new FixedDateTime(Now), new NullLogger<GetSchoolInfoQueryHandler>());
        var publicResult = await publicHandler.Handle(new GetSchoolInfoQuery(slug.Value, token), CancellationToken.None);

        // ————— ADMIN paneldagi belgi —————
        var listHandler = new ListSchoolsQueryHandler(context, executor, new FakeSchoolsAppSettings());
        var listResult = await listHandler.Handle(
            new ListSchoolsQuery(null, null, null, 1, 20, null), CancellationToken.None);

        var linkHealth = listResult.Value.Items.Single().LinkHealth;

        // 1) Kutilgan holat (stsenariy ta'rifi) — ikkalasi ham unga mos bo'lishi kerak.
        publicResult.IsSuccess.Should().Be(kutilganMavjudlik);
        (linkHealth.AvailableProgramCount > 0).Should().Be(kutilganMavjudlik);

        // 2) QULF: ular bir-biriga mos bo'lishi SHART — kutilgan qiymatdan qat'i nazar.
        (linkHealth.AvailableProgramCount > 0).Should().Be(
            publicResult.IsSuccess,
            "admin paneldagi \"havola ishlaydi\" mezoni ommaviy handler mezoni bilan AYNAN bir " +
            "xil bo'lishi shart — ular ajralsa panel yana \"hammasi joyida\" deb yolg'on aytadi");

        if (!publicResult.IsSuccess)
        {
            publicResult.Error.Code.Should().Be(ProblemCodes.NoProgramAvailable);
            linkHealth.Status.Should().NotBe("Ok");
        }
        else
        {
            linkHealth.Status.Should().Be("Ok");
        }
    }

    /// <summary>
    /// Yolg'on ogohlantirish bermaslik ham muhim: dasturi bor maktabda belgi CHIQMASLIGI kerak.
    /// </summary>
    [Fact]
    public async Task DasturiBorMaktab_BelgiChiqmaydi()
    {
        var context = new FakeSchoolsAppDbContext();
        var executor = new SchoolsInlineAsyncQueryExecutor();

        var slug = SchoolSlug.Create("sogʻlom-havola-maktabi").Value;
        var school = School.Create(
            Guid.NewGuid(), "Sog'lom Havola Maktabi", "Toshkent", "Mirzo Ulug'bek", slug, "token-sogʻlom-0123456789abcdefghijkl", Now);
        context.SchoolList.Add(school);

        var (testDefinition, question) = BuildUsableTest();
        context.TestDefinitionList.Add(testDefinition);
        context.QuestionList.Add(question);

        var program = BuildProgram(ProgramStatus.Published, isActive: true, ProgramVisibility.Public);
        context.ProgramList.Add(program);
        context.ProgramTestList.Add(ProgramTest.Create(Guid.NewGuid(), program.Id, testDefinition.Id, 1));

        var handler = new ListSchoolsQueryHandler(context, executor, new FakeSchoolsAppSettings());
        var result = await handler.Handle(new ListSchoolsQuery(null, null, null, 1, 20, null), CancellationToken.None);

        var linkHealth = result.Value.Items.Single().LinkHealth;
        linkHealth.Status.Should().Be("Ok");
        linkHealth.AvailableProgramCount.Should().Be(1);
        linkHealth.UsableProgramCount.Should().Be(1);
    }

    /// <summary>Sabab aniq bo'lishi kerak — admin nima qilishni bilishi uchun.</summary>
    [Fact]
    public async Task DasturdaFaolTestYoq_AlohidaSababKorsatiladi()
    {
        var context = new FakeSchoolsAppDbContext();
        var executor = new SchoolsInlineAsyncQueryExecutor();

        var slug = SchoolSlug.Create("testsiz-dastur-maktabi").Value;
        var school = School.Create(
            Guid.NewGuid(), "Testsiz Dastur Maktabi", "Andijon", "Asaka", slug, "token-testsiz-0123456789abcdefghijklmn", Now);
        context.SchoolList.Add(school);

        // Dastur MAVJUD (`Published` + faol + `Public`), lekin tarkibida yaroqli test yo'q.
        var program = BuildProgram(ProgramStatus.Published, isActive: true, ProgramVisibility.Public);
        context.ProgramList.Add(program);

        var handler = new ListSchoolsQueryHandler(context, executor, new FakeSchoolsAppSettings());
        var result = await handler.Handle(new ListSchoolsQuery(null, null, null, 1, 20, null), CancellationToken.None);

        var linkHealth = result.Value.Items.Single().LinkHealth;
        linkHealth.Status.Should().Be("ProgramsWithoutTests");
        linkHealth.AvailableProgramCount.Should().Be(1);
        linkHealth.UsableProgramCount.Should().Be(0);
    }

    /// <summary>Jonli hodisaning sababi — dastur bor, lekin bu maktabga biriktirilmagan.</summary>
    [Fact]
    public async Task DasturBiriktirilmagan_SababAniqKorsatiladi()
    {
        var context = new FakeSchoolsAppDbContext();
        var executor = new SchoolsInlineAsyncQueryExecutor();

        var slug = SchoolSlug.Create("biriktirilmagan-maktab").Value;
        var school = School.Create(
            Guid.NewGuid(), "Biriktirilmagan Maktab", "Farg'ona", "Qo'qon", slug, "token-biriktirilmagan-0123456789abcdef", Now);
        context.SchoolList.Add(school);

        var program = BuildProgram(ProgramStatus.Published, isActive: true, ProgramVisibility.Assigned);
        context.ProgramList.Add(program);

        var handler = new ListSchoolsQueryHandler(context, executor, new FakeSchoolsAppSettings());
        var result = await handler.Handle(new ListSchoolsQuery(null, null, null, 1, 20, null), CancellationToken.None);

        result.Value.Items.Single().LinkHealth.Status.Should().Be("NoProgramAssigned");
    }

    private static AssessmentProgram BuildProgram(ProgramStatus status, bool isActive, ProgramVisibility visibility)
    {
        var program = AssessmentProgram.Create(
            Guid.NewGuid(), "KRITERIYA_DASTUR", "Kriteriya dasturi", Now, displayOrder: 1, visibility: visibility);

        switch (status)
        {
            case ProgramStatus.Published:
                program.AddTest(Guid.NewGuid(), 1, Now);
                program.Publish(Now);
                break;
            case ProgramStatus.Archived:
                program.Archive(Now);
                break;
            case ProgramStatus.Draft:
            default:
                break;
        }

        if (!isActive)
        {
            program.Deactivate(Now);
        }

        return program;
    }

    /// <summary>Nashr qilingan, faol va kamida bitta faol savoli bor metodika.</summary>
    private static (TestDefinition Definition, Question Question) BuildUsableTest()
    {
        var testDefinitionId = Guid.NewGuid();
        var question = Question.Create(
            Guid.NewGuid(), testDefinitionId, "Q1", 1, "Sinov savoli", QuestionType.Likert5, "EI", 1, 1m, isSystem: true);

        var definition = TestDefinition.CreateSystemPublished(
            testDefinitionId, "KRITERIYA_TEST", "Kriteriya metodikasi", null, 1, 5, false, 10, "likert_sum", [question], Now);

        return (definition, question);
    }

    private sealed class FixedDateTime : IDateTime
    {
        public FixedDateTime(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}

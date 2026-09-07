using FluentAssertions;
using StudentRoadMap.Application.Admin.Schools;
using StudentRoadMap.Application.Admin.Schools.GetById;
using StudentRoadMap.Application.Tests.Admin.Schools.Testing;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Admin.Schools;

/// <summary>
/// `GetSchoolByIdQueryHandler` — maktab ichki sahifasidagi ISHTIROK STATISTIKASI (egasining
/// talabi: "maktabning ichiga kirilganda, o'sha maktabda nechta odam test topshirgani
/// ko'rinishi kerak"). DB'siz sinov (`GetSchoolInfoQueryHandlerTests` / `FakeAiAppDbContext`
/// bilan bir xil naqsh — loyihada mocking kutubxonasi yo'q).
///
/// EF Core darajasidagi tarjima va `IsDeleted` global filtri esa integratsiya testida
/// (`AdminSchoolStatsEndpointTests`) tasdiqlanadi — bu yerda MANTIQ sinaladi.
/// </summary>
public sealed class GetSchoolByIdQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_IshtirokStatistikasi_ToGriHisoblanadi()
    {
        var (context, school) = ArrangeSchool();

        // 4 ta o'quvchi: 2 tasi yakunlagan (snapshot `CompletedAssessmentCount > 0`), 2 tasi yo'q.
        AddStudent(context, school.Id, "Aliyev Vali", "+998901110001", completedCount: 1, lastAssessmentAt: Now.AddDays(-3));
        AddStudent(context, school.Id, "Karimova Zulfiya", "+998901110002", completedCount: 2, lastAssessmentAt: Now.AddDays(-1));
        AddStudent(context, school.Id, "Toshpo'latov Anvar", "+998901110003", completedCount: 0, lastAssessmentAt: null);
        AddStudent(context, school.Id, "Rahimova Nilufar", "+998901110004", completedCount: 0, lastAssessmentAt: null);

        // Boshqa maktab o'quvchisi — statistikaga KIRMASLIGI kerak.
        AddStudent(context, Guid.NewGuid(), "Begona Maktab O'quvchisi", "+998901119999", completedCount: 5, lastAssessmentAt: Now);

        // Sessiyalar: 1 ta `InProgress` (jarayonda), 1 ta `Draft` (hali test boshlanmagan) —
        // faqat birinchisi "jarayonda" deb sanaladi.
        context.AssessmentList.Add(MakeAssessment(school.Id, "in-progress", started: true));
        context.AssessmentList.Add(MakeAssessment(school.Id, "draft", started: false));
        context.AssessmentList.Add(MakeAssessment(Guid.NewGuid(), "boshqa-maktab", started: true));

        var result = await HandleAsync(context, school.Id);

        result.IsSuccess.Should().BeTrue();
        var stats = result.Value.Stats;
        stats.StudentCount.Should().Be(4);
        stats.CompletedCount.Should().Be(2);
        stats.InProgressCount.Should().Be(1, "faqat `Status == InProgress` — `Draft` boshlanmagan sanaladi");
        stats.CompletionRate.Should().Be(0.5, "ULUSH (0..1), foiz EMAS — frontend `× 100` qiladi");
        stats.LastActivityAt.Should().Be(Now.AddDays(-1), "eng so'nggi `LastAssessmentAt`");
    }

    [Fact]
    public async Task Handle_HechKimRoyxatdanOtmagan_CompletionRateNullQaytaradi()
    {
        var (context, school) = ArrangeSchool();

        var result = await HandleAsync(context, school.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stats.StudentCount.Should().Be(0);
        result.Value.Stats.CompletedCount.Should().Be(0);
        result.Value.Stats.InProgressCount.Should().Be(0);
        result.Value.Stats.CompletionRate.Should().BeNull(
            "`registered == 0` — nisbat ANIQLANMAGAN; HAQIQIY 0% bilan chalkashtirilmasin (`docs/06` §8)");
        result.Value.Stats.LastActivityAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HechKimYakunlamagan_CompletionRateNolQaytaradi()
    {
        var (context, school) = ArrangeSchool();
        AddStudent(context, school.Id, "Yakunlamagan O'quvchi", "+998901110010", completedCount: 0, lastAssessmentAt: null);

        var result = await HandleAsync(context, school.Id);

        result.Value.Stats.CompletionRate.Should().Be(0.0, "ro'yxatdan o'tgan, lekin yakunlamagan — bu HAQIQIY 0%, `null` emas");
    }

    [Fact]
    public async Task Handle_OchirilganOquvchiVaSessiya_HisobgaOlinmaydi()
    {
        var (context, school) = ArrangeSchool();
        AddStudent(context, school.Id, "Faol O'quvchi", "+998901110020", completedCount: 1, lastAssessmentAt: Now.AddDays(-2));

        var deleted = MakeStudent(school.Id, "O'chirilgan O'quvchi", "+998901110021");
        deleted.UpdateSnapshot(null, null, null, null, null, false, Now, 1, Now);
        deleted.MarkDeleted(Now);
        context.StudentList.Add(deleted);

        var deletedAssessment = MakeAssessment(school.Id, "o-chirilgan", started: true);
        deletedAssessment.MarkDeleted(Now);
        context.AssessmentList.Add(deletedAssessment);

        var result = await HandleAsync(context, school.Id);

        result.Value.Stats.StudentCount.Should().Be(1, "soft-delete qilingan o'quvchi sanalmaydi");
        result.Value.Stats.CompletedCount.Should().Be(1);
        result.Value.Stats.InProgressCount.Should().Be(0, "soft-delete qilingan sessiya sanalmaydi");
    }

    [Fact]
    public async Task Handle_MaktabTopilmasa_NotFoundQaytaradi()
    {
        var (context, _) = ArrangeSchool();

        var result = await HandleAsync(context, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOT_FOUND");
    }

    private static (FakeSchoolsAppDbContext Context, School School) ArrangeSchool()
    {
        var context = new FakeSchoolsAppDbContext();
        var slug = SchoolSlug.Create("maktab-statistika").Value;
        var school = School.Create(
            Guid.NewGuid(), "12-son maktab", "Farg'ona", "Qo'qon", slug, "access-token-stats-0123456789abcdef", "STAT2345", Now);
        context.SchoolList.Add(school);
        return (context, school);
    }

    private static Task<Result<AdminSchoolDetailDto>> HandleAsync(
        FakeSchoolsAppDbContext context, Guid schoolId)
    {
        var handler = new GetSchoolByIdQueryHandler(
            context, new SchoolsInlineAsyncQueryExecutor(), new FakeSchoolsAppSettings(), new FakeQrCodeGenerator());

        return handler.Handle(new GetSchoolByIdQuery(schoolId), CancellationToken.None);
    }

    private static Student MakeStudent(Guid schoolId, string fullName, string phone) =>
        Student.Create(
            Guid.NewGuid(), schoolId, fullName, new DateOnly(2010, 1, 1), Gender.Male, 9,
            PhoneNumber.Create(phone).Value, Now, Now);

    private static void AddStudent(
        FakeSchoolsAppDbContext context, Guid schoolId, string fullName, string phone, int completedCount, DateTimeOffset? lastAssessmentAt)
    {
        var student = MakeStudent(schoolId, fullName, phone);
        if (lastAssessmentAt.HasValue)
        {
            student.UpdateSnapshot(null, null, null, null, null, false, lastAssessmentAt.Value, completedCount, Now);
        }

        context.StudentList.Add(student);
    }

    /// <summary>`started: false` — `Draft` (test boshlanmagan); `true` — `InProgress`.</summary>
    private static Assessment MakeAssessment(Guid schoolId, string tokenSeed, bool started)
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(), Guid.NewGuid(), schoolId, $"session-token-{tokenSeed}", "uz", Guid.NewGuid(),
            startedAt: Now.AddHours(-1), expiresAt: Now.AddDays(7), now: Now.AddHours(-1));

        if (started)
        {
            var testDefinitionId = Guid.NewGuid();
            assessment.AddTest(AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinitionId, 1, totalCount: 1));
            assessment.StartTest(testDefinitionId, Now.AddHours(-1));
        }

        return assessment;
    }
}

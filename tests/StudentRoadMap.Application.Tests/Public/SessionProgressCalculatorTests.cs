using FluentAssertions;
using StudentRoadMap.Application.Public.GetSession;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `SessionProgressCalculator` — `GetSessionStateQueryHandler`dan ajratilgan "qayerda
/// to'xtagan" qoidasi (2026-09-07). Bu yerda qulflanadigan shart: xatti-harakat AYNAN
/// eskisi bilan bir xil — birinchi `Completed` bo'lmagan test joriy, undan keyingilar
/// `"Locked"`, hamma tugagan bo'lsa joriy yo'q.
/// </summary>
public sealed class SessionProgressCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FindCurrentIndex_BirinchiTugallanmaganTestniQaytaradi()
    {
        var tests = BuildTests(TestStatus.Completed, TestStatus.InProgress, TestStatus.NotStarted, TestStatus.NotStarted);

        SessionProgressCalculator.FindCurrentIndex(tests).Should().Be(1, "4 dan 2-blok — 0 dan sanalgan indeks 1");
        SessionProgressCalculator.CountCompleted(tests).Should().Be(1);
    }

    [Fact]
    public void FindCurrentIndex_HammasiTugallangan_NullQaytaradi()
    {
        var tests = BuildTests(TestStatus.Completed, TestStatus.Completed);

        SessionProgressCalculator.FindCurrentIndex(tests).Should().BeNull();
        SessionProgressCalculator.CountCompleted(tests).Should().Be(2);
    }

    [Fact]
    public void FindCurrentIndex_HechNarsaBoshlanmagan_NolQaytaradi()
    {
        var tests = BuildTests(TestStatus.NotStarted, TestStatus.NotStarted);

        SessionProgressCalculator.FindCurrentIndex(tests).Should().Be(0, "`Draft` sessiya — birinchi blok joriy");
    }

    [Fact]
    public void ProjectStatus_JoriyTestdanKeyingilarLocked()
    {
        var tests = BuildTests(TestStatus.Completed, TestStatus.InProgress, TestStatus.NotStarted);
        var current = SessionProgressCalculator.FindCurrentIndex(tests);

        SessionProgressCalculator.ProjectStatus(tests, 0, current).Should().Be("Completed");
        SessionProgressCalculator.ProjectStatus(tests, 1, current).Should().Be("InProgress");
        SessionProgressCalculator.ProjectStatus(tests, 2, current).Should().Be(SessionProgressCalculator.LockedStatus);
    }

    [Fact]
    public void ProjectStatus_HammasiTugallangan_LockedYoq()
    {
        var tests = BuildTests(TestStatus.Completed, TestStatus.Completed);

        SessionProgressCalculator.ProjectStatus(tests, 1, currentIndex: null).Should().Be("Completed");
    }

    [Fact]
    public void ProgressPercent_YaxlitlanadiVaSavolsizNol()
    {
        var tests = BuildTests(TestStatus.InProgress);
        for (var i = 0; i < 17; i++)
        {
            tests[0].UpsertAnswer(Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        }

        SessionProgressCalculator.ProgressPercent(tests).Should().Be(39, "17/44 = 38.6 → 39");
        SessionProgressCalculator.ProgressPercent([]).Should().Be(0);
    }

    /// <summary>Har blok 44 savolli; `InProgress`/`Completed` holatlar domen metodlari orqali olinadi.</summary>
    private static List<AssessmentTest> BuildTests(params TestStatus[] statuses)
    {
        var assessmentId = Guid.NewGuid();
        var result = new List<AssessmentTest>();

        for (var i = 0; i < statuses.Length; i++)
        {
            var test = AssessmentTest.Create(Guid.NewGuid(), assessmentId, Guid.NewGuid(), displayOrder: i + 1, totalCount: 44);

            if (statuses[i] is TestStatus.InProgress or TestStatus.Completed)
            {
                test.Start(Now);
            }

            if (statuses[i] == TestStatus.Completed)
            {
                test.Complete(Now, requiredQuestionIds: []);
            }

            result.Add(test);
        }

        return result;
    }
}

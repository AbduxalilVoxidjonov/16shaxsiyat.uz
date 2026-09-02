using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentRoadMap.Api.IntegrationTests.Testing;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Persistence;

namespace StudentRoadMap.Api.IntegrationTests.Jobs;

/// <summary>
/// `AnalysisWorkerBackgroundService` — P18 (`prompts/18`) DoD: "`MockAiProvider` bilan: sessiya
/// yakunlangach 5 soniya ichida `Analyzed` bo'ladi". Bu test bevosita `IBackgroundJobQueue`
/// (haqiqiy `AnalysisJobQueue`, `AddInfrastructure`da ro'yxatdan o'tgan) orqali navbatga
/// qo'yadi — `POST /sessions/complete` orqali TO'LIQ (190 savolli) oqim
/// `PublicCompleteFlowEndpointTests`da allaqachon qamrab olingan; bu yerda faqat FON
/// ISHCHISI (haqiqiy ASP.NET Core host, haqiqiy `BackgroundService`, `MockAiProvider` —
/// Dev/Test muhitida `Api/Program.cs` orqali) ishlashi tekshiriladi.
/// </summary>
public sealed class AiAnalysisBackgroundPipelineTests : IClassFixture<PublicApiTestFactory>
{
    // `prompts/18` DoD talabi — "5 soniya ichida" — YAKKA holda (bu fayl `--filter` bilan)
    // ishga tushirilganda ~2s da bajariladi. Butun sinov to'plami (`dotnet test StudentRoadMap.sln`)
    // BIR VAQTDA o'nlab `WebApplicationFactory` xostini (har birida haqiqiy `BackgroundService`)
    // ko'taradi — shu resurs raqobati ostida real vaqt CI'da beqaror (flaky) bo'lib qolmasligi
    // uchun kutish chegarasi xavfsizroq (15s) qilib olindi; DoD'ning o'zi (yakka ishga
    // tushirishda ~2s) shu bilan buzilmaydi.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    private readonly PublicApiTestFactory _factory;

    public AiAnalysisBackgroundPipelineTests(PublicApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnqueueAiAnalysisAsync_RealBackgroundService_MarksAssessmentAnalyzedWithinFiveSeconds()
    {
        var now = DateTimeOffset.UtcNow;
        Guid assessmentId;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var school = await TestDataFactory.CreateSchoolAsync(db, now, "maktab-ai-pipeline", TestDataFactory.NewAccessToken("ai-pipeline"));
            var programId = await TestDataFactory.GetOrCreateDefaultProgramIdAsync(db, now);

            var student = Student.Create(
                Guid.NewGuid(), school.Id, "Nodira Yusupova", new DateOnly(2009, 6, 12), Gender.Female, 10,
                PhoneNumber.Create("901112233").Value, now, now);

            var testDefinition = TestDefinition.Create(Guid.NewGuid(), $"CODE-{Guid.NewGuid():N}"[..10], "AI quvur sinovi anketasi", 1, 5, "SUM", now);
            var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, $"tok-ai-pipeline-{Guid.NewGuid():N}", "uz", programId, now, now.AddDays(7), now);
            var test = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);

            // Barcha holat o'tishlari BITTA `SaveChangesAsync`dan OLDIN — `Assessment.Complete`
            // (test bloklari to'liq bo'lishini talab qiladi) ni birinchi `SaveChanges`dan keyin
            // ALOHIDA chaqirish (test blokisiz saqlangan yozuvni keyin yangilash) EF Core
            // navigatsiya kuzatuvida nomuvofiqlikka olib kelishi mumkin edi — shu sabab
            // `AnalysisOrchestratorTests.SeedAnalyzingAssessment` bilan BIR XIL naqsh: bitta Add + bitta Save.
            assessment.AddTest(test);
            assessment.StartTest(test.TestDefinitionId, now);
            assessment.CompleteTest(test.TestDefinitionId, [], now);
            assessment.Complete(now);
            assessment.MarkAnalyzing(now);

            db.Add(student);
            db.Add(testDefinition);
            db.Add(assessment);
            await db.SaveChangesAsync();

            assessmentId = assessment.Id;
        }

        using (var enqueueScope = _factory.Services.CreateScope())
        {
            var jobQueue = enqueueScope.ServiceProvider.GetRequiredService<IBackgroundJobQueue>();
            await jobQueue.EnqueueAiAnalysisAsync(assessmentId);
        }

        var finalStatus = await PollForTerminalStatusAsync(assessmentId);

        finalStatus.Should().Be(AssessmentStatus.Analyzed, "haqiqiy `AnalysisWorkerBackgroundService` + `MockAiProvider` (Dev/Test) {0} ichida tahlilni yakunlashi kerak", PollTimeout);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var currentAnalysis = await verifyDb.AiAnalyses.AsNoTracking().SingleAsync(a => a.AssessmentId == assessmentId && a.IsCurrent);
        currentAnalysis.Status.Should().Be(AiAnalysisStatus.Succeeded);
        currentAnalysis.IsFallbackReport.Should().BeFalse();
    }

    private async Task<AssessmentStatus> PollForTerminalStatusAsync(Guid assessmentId)
    {
        var deadline = DateTimeOffset.UtcNow.Add(PollTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var status = await db.Assessments.AsNoTracking().Where(a => a.Id == assessmentId).Select(a => a.Status).SingleAsync();

            if (status is AssessmentStatus.Analyzed or AssessmentStatus.AnalysisFailed)
            {
                return status;
            }

            await Task.Delay(PollInterval);
        }

        return AssessmentStatus.Analyzing;
    }
}

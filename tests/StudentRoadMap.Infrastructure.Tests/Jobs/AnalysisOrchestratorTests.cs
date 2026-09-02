using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Jobs;
using StudentRoadMap.Infrastructure.Persistence;
using StudentRoadMap.Infrastructure.Tests.Jobs.Testing;
using StudentRoadMap.Infrastructure.Tests.Testing;

namespace StudentRoadMap.Infrastructure.Tests.Jobs;

/// <summary>
/// `AnalysisOrchestrator` — `docs/09-ai-analiz-moduli.md` 1 va 7-bo'lim (retry/fallback zanjiri),
/// `prompts/18` DoD. Haqiqiy `AppDbContext` (SQLite in-memory, boshqa `*Tests`dagi bilan bir xil
/// naqsh) + soxta `IAiProviderResolver`/`IAiAnalysisProvider` (`Jobs/Testing`) — providerning
/// HTTP qatlami emas, ORKESTRATSIYA mantig'i tekshiriladi (HTTP qatlami `GeminiProviderTests`
/// va h.k.da alohida qamrab olingan).
/// </summary>
public sealed class AnalysisOrchestratorTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly Func<TimeSpan, CancellationToken, Task> NoWaitDelay = (_, _) => Task.CompletedTask;

    private static AppDbContext NewContext(SqliteConnection connection) =>
        SqliteAppDbContextFactory.CreateContext(connection, new FixedDateTimeProvider(Now), new NoOpPublisher());

    private static AnalysisOrchestrator CreateOrchestrator(
        AppDbContext context,
        FakeAiProviderResolver resolver,
        IEnumerable<StudentRoadMap.Application.Common.Interfaces.IAiAnalysisProvider>? directProviders = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null) =>
        new(
            context,
            new EfAsyncQueryExecutor(),
            new FixedDateTimeProvider(Now),
            new FakePromptBuilder(),
            resolver,
            new AiResponseValidator(new ConfigurationBuilder().Build()),
            new AiCostCalculator(new ConfigurationBuilder().Build()),
            directProviders ?? [],
            NullLogger<AnalysisOrchestrator>.Instance,
            delay ?? NoWaitDelay);

    /// <summary>
    /// `Assessment` ni `Analyzing` holatiga (test bloklari to'liq yakunlangan holda) yetkazadi —
    /// `AnalysisOrchestrator.RunAsync`ning kirish sharti. FK yaxlitligi uchun `School`/
    /// `AssessmentProgram`/`TestDefinition` ham to'liq (minimal, lekin haqiqiy) qatorlar bilan
    /// yaratiladi (SQLite'da ham FK majburiy — `PromptBuilderTests`dan farqli, bu yerda
    /// entity'lar HAQIQATAN saqlanadi, chunki orkestrator `SaveChangesAsync` chaqiradi).
    /// </summary>
    private static (Assessment Assessment, Student Student) SeedAnalyzingAssessment(AppDbContext context)
    {
        var school = School.Create(
            Guid.NewGuid(), "Test maktabi", "Toshkent", "Chilonzor",
            SchoolSlug.Create($"test-maktabi-{Guid.NewGuid():N}").Value,
            $"access-{Guid.NewGuid():N}", Now);

        var student = Student.Create(
            Guid.NewGuid(), school.Id, "Alisher Karimov", new DateOnly(2010, 5, 20), Gender.Male, 9,
            PhoneNumber.Create("901234567").Value, Now, Now);

        var program = AssessmentProgram.Create(Guid.NewGuid(), $"PROG-{Guid.NewGuid():N}"[..12], "Test dasturi", Now);

        var testDefinition = TestDefinition.Create(Guid.NewGuid(), $"CODE-{Guid.NewGuid():N}"[..10], "Test anketasi", 1, 5, "SUM", Now);

        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, $"tok-{Guid.NewGuid():N}", "uz", program.Id, Now, Now.AddDays(7), Now);
        var test = AssessmentTest.Create(Guid.NewGuid(), assessment.Id, testDefinition.Id, 1, totalCount: 1);
        assessment.AddTest(test);
        assessment.StartTest(test.TestDefinitionId, Now);
        assessment.CompleteTest(test.TestDefinitionId, [], Now);
        assessment.Complete(Now);
        assessment.MarkAnalyzing(Now);

        context.Add(school);
        context.Add(student);
        context.Add(program);
        context.Add(testDefinition);
        context.Add(assessment);

        return (assessment, student);
    }

    [Fact]
    public async Task RunAsync_AssessmentNotAnalyzing_DoesNothing()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        var school = School.Create(
            Guid.NewGuid(), "Test maktabi", "Toshkent", "Chilonzor",
            SchoolSlug.Create($"test-maktabi-{Guid.NewGuid():N}").Value,
            $"access-{Guid.NewGuid():N}", Now);
        var student = Student.Create(Guid.NewGuid(), school.Id, "Botir Yusupov", new DateOnly(2010, 1, 1), Gender.Male, 9, PhoneNumber.Create("901234567").Value, Now, Now);
        var program = AssessmentProgram.Create(Guid.NewGuid(), $"PROG-{Guid.NewGuid():N}"[..12], "Test dasturi", Now);
        var assessment = Assessment.Create(Guid.NewGuid(), student.Id, school.Id, "tok-1", "uz", program.Id, Now, Now.AddDays(7), Now);
        // `Draft` holatida qoldiriladi — `Analyzing` EMAS.
        context.Add(school);
        context.Add(student);
        context.Add(program);
        context.Add(assessment);
        await context.SaveChangesAsync();

        var provider = new ScriptedAiProvider(AiProvider.Gemini, ScriptedAiProvider.Success());
        var resolver = new FakeAiProviderResolver().Add(provider, isDefault: true);
        var orchestrator = CreateOrchestrator(context, resolver);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        provider.CallCount.Should().Be(0);
        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.Draft);
    }

    [Fact]
    public async Task RunAsync_DefaultProviderSucceeds_MarksAnalyzedAndSavesSingleCurrentAnalysis()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);
        await context.SaveChangesAsync();

        var provider = new ScriptedAiProvider(AiProvider.Gemini, ScriptedAiProvider.Success());
        var resolver = new FakeAiProviderResolver().Add(provider, isDefault: true);
        var orchestrator = CreateOrchestrator(context, resolver);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.Analyzed);

        var analyses = context.AiAnalyses.Where(a => a.AssessmentId == assessment.Id).ToList();
        analyses.Should().ContainSingle();
        analyses[0].Status.Should().Be(AiAnalysisStatus.Succeeded);
        analyses[0].IsCurrent.Should().BeTrue();
        analyses[0].IsFallbackReport.Should().BeFalse();
        analyses[0].InputTokens.Should().Be(1500);
    }

    [Fact]
    public async Task RunAsync_TimeoutThenSuccess_RetriesSameProviderAndSucceeds()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);
        await context.SaveChangesAsync();

        var provider = new ScriptedAiProvider(
            AiProvider.Gemini,
            ScriptedAiProvider.Error(AiErrorKind.Timeout),
            ScriptedAiProvider.Success());
        var resolver = new FakeAiProviderResolver().Add(provider, isDefault: true);

        var delayCalls = new List<TimeSpan>();
        var orchestrator = CreateOrchestrator(context, resolver, delay: (ts, _) => { delayCalls.Add(ts); return Task.CompletedTask; });

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        provider.CallCount.Should().Be(2);
        delayCalls.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(2));

        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.Analyzed);

        var analyses = context.AiAnalyses.Where(a => a.AssessmentId == assessment.Id).OrderBy(a => a.AttemptNumber).ToList();
        analyses.Should().HaveCount(2);
        analyses[0].Status.Should().Be(AiAnalysisStatus.Failed);
        analyses[1].Status.Should().Be(AiAnalysisStatus.Succeeded);
        analyses[1].IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task RunAsync_AuthError_MovesToNextProviderWithoutDelay()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);
        await context.SaveChangesAsync();

        var primary = new ScriptedAiProvider(AiProvider.Gemini, ScriptedAiProvider.Error(AiErrorKind.Auth));
        var fallback = new ScriptedAiProvider(AiProvider.OpenAi, ScriptedAiProvider.Success());
        var resolver = new FakeAiProviderResolver().Add(primary, isDefault: true, fallbackOrder: 10).Add(fallback, fallbackOrder: 20);

        var delayCalls = 0;
        var orchestrator = CreateOrchestrator(context, resolver, delay: (_, _) => { delayCalls++; return Task.CompletedTask; });

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(1);
        delayCalls.Should().Be(0);

        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.Analyzed);

        var current = context.AiAnalyses.Single(a => a.AssessmentId == assessment.Id && a.IsCurrent);
        current.Provider.Should().Be(AiProvider.OpenAi);
    }

    [Fact]
    public async Task RunAsync_AllProvidersBadRequest_OverridesLastMessageAndCreatesFallbackReport()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);
        await context.SaveChangesAsync();

        var primary = new ScriptedAiProvider(AiProvider.Gemini, ScriptedAiProvider.Error(AiErrorKind.BadRequest, "provider-specific raw error"));
        var fallback = new ScriptedAiProvider(AiProvider.OpenAi, ScriptedAiProvider.Error(AiErrorKind.BadRequest, "provider-specific raw error 2"));
        var resolver = new FakeAiProviderResolver().Add(primary, isDefault: true, fallbackOrder: 10).Add(fallback, fallbackOrder: 20);

        var orchestrator = CreateOrchestrator(context, resolver);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.AnalysisFailed);

        // `AttemptNumber` bo'yicha — `CreatedAt` testda barcha yozuv uchun bir xil (`FixedDateTimeProvider`).
        var analyses = context.AiAnalyses.Where(a => a.AssessmentId == assessment.Id).OrderBy(a => a.AttemptNumber).ToList();
        // 2 ta haqiqiy urinish (har provider BadRequest'da retry qilmaydi) + 1 ta zaxira hisobot.
        var realAttempts = analyses.Where(a => !a.IsFallbackReport).ToList();
        realAttempts.Should().HaveCount(2);
        realAttempts.Should().OnlyContain(a => a.Status == AiAnalysisStatus.Failed);

        // CLAUDE.md MAXSUS DIQQAT #1: oxirgi urinishning xabari umumiy emas, aniq "so'rov shakli" xabari.
        realAttempts.Last().ErrorMessage.Should().Contain("So'rov shakli noto'g'ri");
        realAttempts.Last().ErrorMessage.Should().NotContain("provider-specific raw error");

        var fallbackReport = analyses.Single(a => a.IsFallbackReport);
        fallbackReport.IsCurrent.Should().BeTrue();
        fallbackReport.Status.Should().Be(AiAnalysisStatus.Succeeded);
    }

    [Fact]
    public async Task RunAsync_NoProviderConfiguredAndNoDirectProvider_CreatesFallbackReportOnly()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);
        await context.SaveChangesAsync();

        var resolver = new FakeAiProviderResolver(); // bo'sh — hech qanday provider yo'q
        var orchestrator = CreateOrchestrator(context, resolver);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.AnalysisFailed);

        var analyses = context.AiAnalyses.Where(a => a.AssessmentId == assessment.Id).ToList();
        analyses.Should().ContainSingle(); // faqat zaxira hisobot, haqiqiy urinish yo'q
        analyses[0].IsFallbackReport.Should().BeTrue();
        analyses[0].IsCurrent.Should().BeTrue();
        // Hech qaysi provider sinalmagani uchun "BadRequest" xabari EMAS.
        analyses[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_ResolverEmpty_FallsBackToDirectlyRegisteredProvider()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);
        await context.SaveChangesAsync();

        var resolver = new FakeAiProviderResolver(); // DB'da konfiguratsiya yo'q
        var directProvider = new ScriptedAiProvider(AiProvider.Gemini, ScriptedAiProvider.Success());
        var orchestrator = CreateOrchestrator(context, resolver, directProviders: [directProvider]);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        directProvider.CallCount.Should().Be(1);
        var reloaded = await context.Assessments.FindAsync(assessment.Id);
        reloaded!.Status.Should().Be(AssessmentStatus.Analyzed);
    }

    [Fact]
    public async Task RunAsync_ExistingCurrentAnalysis_SuccessSupersedesIt()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);

        var oldAnalysis = AiAnalysis.Create(Guid.NewGuid(), assessment.Id, AiProvider.Gemini, "old-model", "v0.9", Now.AddDays(-1));
        oldAnalysis.Start();
        oldAnalysis.Succeed("{}", "eski xulosa", "eski portret", Now.AddDays(-1));
        context.Add(oldAnalysis);
        await context.SaveChangesAsync();

        var provider = new ScriptedAiProvider(AiProvider.Anthropic, ScriptedAiProvider.Success());
        var resolver = new FakeAiProviderResolver().Add(provider, isDefault: true);
        var orchestrator = CreateOrchestrator(context, resolver);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        var reloadedOld = await context.AiAnalyses.FindAsync(oldAnalysis.Id);
        reloadedOld!.IsCurrent.Should().BeFalse();

        var newCurrent = context.AiAnalyses.Single(a => a.AssessmentId == assessment.Id && a.IsCurrent);
        newCurrent.Id.Should().NotBe(oldAnalysis.Id);
        newCurrent.Provider.Should().Be(AiProvider.Anthropic);
    }

    [Fact]
    public async Task RunAsync_ExistingCurrentAnalysis_FallbackReportDoesNotOverwriteIt()
    {
        using var connection = SqliteAppDbContextFactory.CreateOpenConnection();
        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var (assessment, _) = SeedAnalyzingAssessment(context);

        var oldAnalysis = AiAnalysis.Create(Guid.NewGuid(), assessment.Id, AiProvider.Gemini, "old-model", "v0.9", Now.AddDays(-1));
        oldAnalysis.Start();
        oldAnalysis.Succeed("{}", "eski xulosa", "eski portret", Now.AddDays(-1));
        context.Add(oldAnalysis);
        await context.SaveChangesAsync();

        var provider = new ScriptedAiProvider(AiProvider.Gemini, ScriptedAiProvider.Error(AiErrorKind.Server));
        var resolver = new FakeAiProviderResolver().Add(provider, isDefault: true);
        var orchestrator = CreateOrchestrator(context, resolver);

        await orchestrator.RunAsync(assessment.Id, null, null, CancellationToken.None);

        var reloadedOld = await context.AiAnalyses.FindAsync(oldAnalysis.Id);
        reloadedOld!.IsCurrent.Should().BeTrue("eski muvaffaqiyatli tahlil zaxira shablon bilan yashirilmasligi kerak");

        var fallbackReport = context.AiAnalyses.Single(a => a.AssessmentId == assessment.Id && a.IsFallbackReport);
        fallbackReport.IsCurrent.Should().BeFalse();
    }
}

using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Tests.Jobs.Testing;

/// <summary>
/// `AnalysisOrchestrator` testlari uchun — haqiqiy `PromptBuilder` `TypeCatalog`/`CareerMap`
/// so'rovlariga bog'liq (`PromptBuilderTests`da alohida tekshiriladi). Bu yerda orkestratorning
/// RETRY/FALLBACK mantig'i tekshiriladi, prompt qurilishi EMAS — shu sabab doim bir xil,
/// oldindan tayyor natija qaytadi.
/// </summary>
internal sealed class FakePromptBuilder : IPromptBuilder
{
    public const string SystemText = "SYSTEM-TEST";
    public const string UserText = "USER-TEST {ANALYSIS_INPUT_JSON}";
    public const string PromptVersion = "v-test";
    public const string AnalysisInputJson = "{\"context\":{}}";

    public Task<PromptBuildResult> BuildAsync(
        Student student,
        Assessment assessment,
        IReadOnlyList<TestResult> testResults,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PromptBuildResult(SystemText, UserText, PromptVersion, AnalysisInputJson));
}

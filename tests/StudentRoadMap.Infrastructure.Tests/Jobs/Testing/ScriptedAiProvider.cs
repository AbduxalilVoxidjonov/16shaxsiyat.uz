using System.Text.Json;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;

namespace StudentRoadMap.Infrastructure.Tests.Jobs.Testing;

/// <summary>
/// `AnalysisOrchestrator` testlari uchun — oldindan yozilgan ketma-ket natijalarni qaytaradi
/// (masalan: 1-chaqiruv Timeout, 2-chaqiruv muvaffaqiyat). Muvaffaqiyat natijasi UCHUN haqiqiy
/// `MockAiProvider`ning tayyor, `AnalysisJsonSchema`ga to'liq mos JSON'idan foydalanadi — shu
/// bilan `AiResponseValidator`ning HAQIQIY (real) validatsiyasidan o'tadi.
/// </summary>
internal sealed class ScriptedAiProvider : IAiAnalysisProvider
{
    private static readonly MockAiProvider GoodJsonSource = new();

    private readonly Queue<Func<Task<AiCompletionResult>>> _script;

    public ScriptedAiProvider(AiProvider kind, params Func<Task<AiCompletionResult>>[] script)
    {
        Kind = kind;
        _script = new Queue<Func<Task<AiCompletionResult>>>(script);
    }

    public AiProvider Kind { get; }

    public int CallCount { get; private set; }

    public static Func<Task<AiCompletionResult>> Success(int inputTokens = 1500, int outputTokens = 2000) => async () =>
    {
        var real = await GoodJsonSource.CompleteJsonAsync(DummyRequest(), CancellationToken.None).ConfigureAwait(false);
        return real with { InputTokens = inputTokens, OutputTokens = outputTokens };
    };

    public static Func<Task<AiCompletionResult>> Error(AiErrorKind kind, string message = "Simulyatsiya qilingan xato.") =>
        () => Task.FromResult(new AiCompletionResult(false, null, null, null, 10, message, kind));

    /// <summary>`AnalysisJsonSchema`ga mos EMAS javob — validator "Retry" qaytarishi kerak bo'lgan ssenariylar uchun.</summary>
    public static Func<Task<AiCompletionResult>> InvalidSchema() =>
        () => Task.FromResult(new AiCompletionResult(true, """{"summary":"juda qisqa"}""", 100, 100, 5, null, AiErrorKind.None));

    public Task<AiCompletionResult> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        CallCount++;
        if (_script.Count == 0)
        {
            throw new InvalidOperationException($"ScriptedAiProvider ({Kind}): skript tugagan, lekin yana chaqirildi (chaqiruv #{CallCount}).");
        }

        return _script.Dequeue()();
    }

    public Task<AiHealthResult> CheckHealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new AiHealthResult(true, "ScriptedAiProvider — doim sog'lom."));

    private static AiCompletionRequest DummyRequest() =>
        new("system", "user", JsonDocument.Parse("{}"), "test-model", 100, 0.4);
}

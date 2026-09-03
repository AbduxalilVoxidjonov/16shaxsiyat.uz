using FluentAssertions;
using StudentRoadMap.Application.Admin.Ai.TestProvider;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Tests.Admin.Ai.Testing;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Tests.Admin.Ai;

/// <summary>
/// `TestAiProviderCommandHandler` — `docs/07-api-shartnoma.md` §3.5 "POST .../test".
/// <para>
/// ⚠️ Koordinator ko'rsatmasi (2026-09-02, P28 UI xom `message`ni to'g'ridan-to'g'ri ko'rsatadi):
/// (1) xabar `AiErrorKind` bo'yicha aniq, harakatga yo'naltiruvchi va o'zbekcha bo'lishi;
/// (2) provayder javobining XOM matni (potentsial kalit sizib chiqishi bilan) `message`ga
/// HECH QACHON qo'yilmasligi kerak — quyidagi `MessageNeverContainsRawProviderTextOrKey`
/// testi buni qulflaydi.
/// </para>
/// </summary>
public sealed class TestAiProviderCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
    private const string SecretApiKey = "sk-super-secret-key-0123456789";

    private sealed class FakeAiAnalysisProvider : IAiAnalysisProvider
    {
        private readonly AiHealthResult _health;

        public FakeAiAnalysisProvider(AiProvider kind, AiHealthResult health)
        {
            Kind = kind;
            _health = health;
        }

        public AiProvider Kind { get; }

        public Task<AiCompletionResult> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Test endpointi CompleteJsonAsync chaqirmasligi kerak.");

        public Task<AiHealthResult> CheckHealthAsync(CancellationToken cancellationToken) => Task.FromResult(_health);
    }

    private sealed class FakeAiProviderResolver : IAiProviderResolver
    {
        private readonly IAiAnalysisProvider? _provider;

        public FakeAiProviderResolver(IAiAnalysisProvider? provider) => _provider = provider;

        public Task<IReadOnlyList<AiProviderInfo>> GetAvailableAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IAiAnalysisProvider> ResolveAsync(AiProvider? requested, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Test endpointi ResolveAsync EMAS, ResolveForTestAsync chaqiradi.");

        public Task<IReadOnlyList<IAiAnalysisProvider>> GetFallbackChainAsync(AiProvider primary, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IAiAnalysisProvider> ResolveForTestAsync(AiProvider provider, CancellationToken cancellationToken) =>
            _provider is null
                ? throw new InvalidOperationException($"'{provider}' uchun API kaliti kiritilmagan.")
                : Task.FromResult(_provider);
    }

    private static TestAiProviderCommandHandler CreateHandler(IAiAnalysisProvider? provider, FakeAiAppDbContext? context = null) =>
        new(new FakeAiProviderResolver(provider), context ?? new FakeAiAppDbContext(), new InlineAsyncQueryExecutor(), new FakeDateTime(Now));

    [Fact]
    public async Task Handle_ProviderHealthy_ReturnsOkWithGenericSuccessMessage()
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Gemini, new AiHealthResult(true, "Gemini ulanishi muvaffaqiyatli (42 ms)."));
        var handler = CreateHandler(provider);

        var result = await handler.Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Ok.Should().BeTrue();
        result.Value.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(AiErrorKind.Auth, "API kaliti")]
    [InlineData(AiErrorKind.RateLimit, "limiti")]
    [InlineData(AiErrorKind.Timeout, "javob bermadi")]
    [InlineData(AiErrorKind.Server, "javob bermadi")]
    [InlineData(AiErrorKind.BadRequest, "So'rov shakli")]
    [InlineData(AiErrorKind.ModelNotFound, "Model topilmadi")]
    [InlineData(AiErrorKind.Network, "ulanib bo'lmadi")]
    [InlineData(AiErrorKind.Schema, "shakldagi javob")]
    public async Task Handle_ProviderUnhealthy_ReturnsKindSpecificUzbekMessage(AiErrorKind kind, string expectedSubstring)
    {
        // Provayderning xom xabari ATAYLAB sirni o'z ichiga oladi — handler buni QATIYAN ishlatmasligi kerak.
        var provider = new FakeAiAnalysisProvider(AiProvider.OpenAi, new AiHealthResult(false, $"raw provider body containing {SecretApiKey}", kind));
        var handler = CreateHandler(provider);

        var result = await handler.Handle(new TestAiProviderCommand(AiProvider.OpenAi, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("ulanish sinovi o'zi muvaffaqiyatsiz bo'lsa ham HTTP darajasida muvaffaqiyat — natija `ok:false`");
        result.Value.Ok.Should().BeFalse();
        result.Value.Message.Should().Contain(expectedSubstring);
    }

    /// <summary>Koordinator talabi: `message`da provayder javobining xom matni (potentsial kalit) HECH QACHON bo'lmasligi kerak.</summary>
    [Theory]
    [InlineData(AiErrorKind.Auth)]
    [InlineData(AiErrorKind.RateLimit)]
    [InlineData(AiErrorKind.Timeout)]
    [InlineData(AiErrorKind.Server)]
    [InlineData(AiErrorKind.BadRequest)]
    [InlineData(AiErrorKind.Unknown)]
    [InlineData(AiErrorKind.ModelNotFound)]
    [InlineData(AiErrorKind.Network)]
    [InlineData(AiErrorKind.Schema)]
    public async Task Handle_ProviderUnhealthy_MessageNeverContainsRawProviderTextOrKey(AiErrorKind kind)
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Anthropic, new AiHealthResult(false, $"leaked key fragment: {SecretApiKey}", kind));
        var handler = CreateHandler(provider);

        var result = await handler.Handle(new TestAiProviderCommand(AiProvider.Anthropic, Guid.NewGuid()), CancellationToken.None);

        result.Value.Message.Should().NotContain(SecretApiKey);
        result.Value.Message.Should().NotContain("leaked key fragment");
    }

    /// <summary>
    /// Yangi `AiErrorKind` qo'shilib, unga xabar yozilmasa — bu test yiqiladi (barcha turlar
    /// uchun bo'sh bo'lmagan, xom matndan farqli xabar bo'lishi kafolatlanadi).
    /// </summary>
    [Fact]
    public async Task Handle_EveryErrorKind_ProducesNonEmptySafeMessage()
    {
        foreach (var kind in Enum.GetValues<AiErrorKind>().Where(k => k != AiErrorKind.None))
        {
            var provider = new FakeAiAnalysisProvider(AiProvider.Gemini, new AiHealthResult(false, $"raw {SecretApiKey}", kind));
            var handler = CreateHandler(provider);

            var result = await handler.Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

            result.Value.Message.Should().NotBeNullOrWhiteSpace($"'{kind}' uchun xabar yozilishi kerak");
            result.Value.Message.Should().NotContain(SecretApiKey);
        }
    }

    [Fact]
    public async Task Handle_ProviderNotConfigured_ReturnsNotFoundError()
    {
        var handler = CreateHandler(null);

        var result = await handler.Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(StudentRoadMap.Application.Common.Models.ProblemCodes.NotFound);
    }

    [Fact]
    public async Task Handle_ExistingConfig_RecordsConnectionCheckAndSaves()
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Gemini, new AiHealthResult(true, "ok"));
        var context = new FakeAiAppDbContext();
        var config = AiProviderConfig.Create(Guid.NewGuid(), AiProvider.Gemini, "Gemini", "gemini-2.0-flash", Now);
        context.ProviderConfigList.Add(config);

        var handler = CreateHandler(provider, context);

        await handler.Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

        config.LastCheckStatus.Should().Be("ok");
        config.LastCheckedAt.Should().Be(Now);
        context.SaveChangesCalled.Should().BeTrue();
    }
}

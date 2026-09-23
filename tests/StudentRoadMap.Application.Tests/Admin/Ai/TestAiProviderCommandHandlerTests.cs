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
    [InlineData(AiErrorKind.Timeout, "javob bermadi (timeout)")]
    [InlineData(AiErrorKind.Server, "Provayder xatosi")]
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

    /// <summary>2026-09-23: Gemini 503 "overloaded" timeout bilan bir xil xabar berardi — endi alohida.</summary>
    [Fact]
    public async Task Handle_Gemini503_ReturnsOverloadedMessageWithDetail()
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Gemini, new AiHealthResult(
            false, "raw", AiErrorKind.Server, StatusCode: 503, ProviderDetail: "The model is overloaded. Please try again later.", Model: "gemini-3.1-flash-lite"));

        var result = await CreateHandler(provider).Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

        result.Value.Ok.Should().BeFalse();
        result.Value.Message.Should().StartWith("Google serverlari hozir band (503)");
        result.Value.Message.Should().Contain("boshqa modelni tanlang");
        result.Value.Message.Should().EndWith("Tafsilot: The model is overloaded. Please try again later.");
        result.Value.Message.Should().NotContain("timeout");
    }

    [Theory]
    [InlineData(500, "Provayder xatosi (kod 500)")]
    [InlineData(502, "Provayder xatosi (kod 502)")]
    public async Task Handle_Other5xx_ReturnsProviderErrorWithCode(int status, string expected)
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.OpenAi, new AiHealthResult(false, "raw", AiErrorKind.Server, StatusCode: status));

        var result = await CreateHandler(provider).Handle(new TestAiProviderCommand(AiProvider.OpenAi, Guid.NewGuid()), CancellationToken.None);

        result.Value.Message.Should().StartWith(expected);
        result.Value.Message.Should().NotContain("Tafsilot");
    }

    [Fact]
    public async Task Handle_Anthropic529_ReturnsOverloadedMessage()
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Anthropic, new AiHealthResult(false, "raw", AiErrorKind.Server, StatusCode: 529, ProviderDetail: "Overloaded"));

        var result = await CreateHandler(provider).Handle(new TestAiProviderCommand(AiProvider.Anthropic, Guid.NewGuid()), CancellationToken.None);

        result.Value.Message.Should().StartWith("Anthropic serverlari hozir band (529)");
    }

    [Fact]
    public async Task Handle_ModelNotFound_NamesTheModel()
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Gemini, new AiHealthResult(
            false, "raw", AiErrorKind.ModelNotFound, StatusCode: 404, ProviderDetail: "models/gemini-2.0-flash is not found for API version v1beta", Model: "gemini-2.0-flash"));

        var result = await CreateHandler(provider).Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

        result.Value.Message.Should().StartWith("'gemini-2.0-flash' modeli topilmadi — model nomini tekshiring.");
        result.Value.Message.Should().Contain("Tafsilot: models/gemini-2.0-flash is not found");
    }

    /// <summary>Tafsilotga kalitga o'xshash satr tushib qolsa ham (Infrastructure tozalashidan o'tib ketgan holat) — xabarga tushmaydi.</summary>
    [Fact]
    public async Task Handle_DetailWithKeyLikeText_IsScrubbed()
    {
        var provider = new FakeAiAnalysisProvider(AiProvider.Gemini, new AiHealthResult(
            false, "raw", AiErrorKind.Server, StatusCode: 503, ProviderDetail: $"bad key {SecretApiKey} and AIzaSyD-verysecretkey1234567890"));

        var result = await CreateHandler(provider).Handle(new TestAiProviderCommand(AiProvider.Gemini, Guid.NewGuid()), CancellationToken.None);

        result.Value.Message.Should().NotContain(SecretApiKey);
        result.Value.Message.Should().NotContain("AIzaSyD-verysecretkey1234567890");
        result.Value.Message.Should().Contain("Tafsilot: bad key ***");
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

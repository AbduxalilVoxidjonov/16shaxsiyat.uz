using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// "Aloqani tekshirish" (2026-09-23): Gemini `503 UNAVAILABLE "The model is overloaded"` timeout
/// bilan bir xil xabar berardi. Endi: (1) `CheckHealthAsync` vaqtinchalik xatoda 2 marta qayta
/// urinadi (1 s, 3 s; 429 — faqat `Retry-After` ≤ 5 s bo'lsa); (2) natijada status kodi va
/// tozalangan `error.message` qaytadi; (3) fon tahlili yo'li (`CompleteJsonAsync`) HTTP
/// darajasida qayta URINMAYDI — u yerda `AnalysisOrchestrator`ning o'z retry'i bor.
/// </summary>
public sealed class HealthCheckRetryTests
{
    private const string ApiKey = "AIzaFAKEKEYFORTESTS1234567890abcdefghi";
    private const string Model = "gemini-3.1-flash-lite";

    private const string OverloadedBody = """{"error":{"code":503,"message":"The model is overloaded. Please try again later.","status":"UNAVAILABLE"}}""";
    private const string HealthyBody = """{"candidates":[{"content":{"parts":[{"text":"{\"ok\":true}"}]}}]}""";

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses;

        public SequenceHandler(params Func<HttpResponseMessage>[] responses) => _responses = new Queue<Func<HttpResponseMessage>>(responses);

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var next = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return Task.FromResult(next());
        }
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode status, string body, TimeSpan? retryAfter = null) => () =>
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
        if (retryAfter is { } ra)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(ra);
        }

        return response;
    };

    private static (GeminiProvider Provider, List<TimeSpan> Delays) CreateGemini(HttpMessageHandler handler)
    {
        var delays = new List<TimeSpan>();
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model, delay: (d, _) =>
        {
            delays.Add(d);
            return Task.CompletedTask;
        });
        return (provider, delays);
    }

    [Fact]
    public async Task CheckHealthAsync_503Overloaded_RetriesTwiceThenReturnsStatusAndDetail()
    {
        var handler = new SequenceHandler(Respond(HttpStatusCode.ServiceUnavailable, OverloadedBody));
        var (provider, delays) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeFalse();
        health.ErrorKind.Should().Be(AiErrorKind.Server);
        health.StatusCode.Should().Be(503);
        health.ProviderDetail.Should().Be("The model is overloaded. Please try again later.");
        health.Model.Should().Be(Model);
        handler.CallCount.Should().Be(3, "1 asosiy + 2 qayta urinish");
        delays.Should().Equal(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task CheckHealthAsync_503ThenOk_SucceedsOnRetry()
    {
        var handler = new SequenceHandler(
            Respond(HttpStatusCode.ServiceUnavailable, OverloadedBody),
            Respond(HttpStatusCode.OK, HealthyBody));
        var (provider, delays) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeTrue();
        handler.CallCount.Should().Be(2);
        delays.Should().Equal(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CheckHealthAsync_404ModelNotFound_NoRetryAndReturnsModel()
    {
        var handler = new SequenceHandler(Respond(HttpStatusCode.NotFound,
            """{"error":{"code":404,"message":"models/gemini-2.0-flash is not found for API version v1beta, or is not supported for generateContent.","status":"NOT_FOUND"}}"""));
        var (provider, delays) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.ErrorKind.Should().Be(AiErrorKind.ModelNotFound);
        health.StatusCode.Should().Be(404);
        health.Model.Should().Be(Model);
        handler.CallCount.Should().Be(1);
        delays.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_429WithoutRetryAfter_NoRetry()
    {
        var handler = new SequenceHandler(Respond((HttpStatusCode)429, """{"error":{"message":"Quota exceeded","status":"RESOURCE_EXHAUSTED"}}"""));
        var (provider, delays) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.ErrorKind.Should().Be(AiErrorKind.RateLimit);
        handler.CallCount.Should().Be(1, "sarlavhasiz 429 odatda kvota tugagani — qayta urinish befoyda");
        delays.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_429WithShortRetryAfter_HonorsHeader()
    {
        var handler = new SequenceHandler(
            Respond((HttpStatusCode)429, """{"error":{"message":"slow down"}}""", TimeSpan.FromSeconds(2)),
            Respond(HttpStatusCode.OK, HealthyBody));
        var (provider, delays) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeTrue();
        delays.Should().Equal(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CheckHealthAsync_RetryAfterTooLong_NoRetry()
    {
        var handler = new SequenceHandler(Respond(HttpStatusCode.ServiceUnavailable, OverloadedBody, TimeSpan.FromSeconds(60)));
        var (provider, delays) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeFalse();
        handler.CallCount.Should().Be(1);
        delays.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_BudgetExceeded_StopsRetrying()
    {
        var handler = new SequenceHandler(Respond(HttpStatusCode.ServiceUnavailable, OverloadedBody));
        var delays = new List<TimeSpan>();
        var factory = new TimeoutHttpClientFactory(handler, TimeSpan.FromMilliseconds(500));
        var provider = new GeminiProvider(factory, ApiKey, Model, delay: (d, _) =>
        {
            delays.Add(d);
            return Task.CompletedTask;
        });

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeFalse();
        handler.CallCount.Should().Be(1, "1 s kutish 0.5 s umumiy byudjetdan oshadi");
        delays.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_KeyInProviderMessage_NeverLeaksToResult()
    {
        var body = $$$"""{"error":{"code":503,"message":"overloaded for key {{{ApiKey}}} and sk-proj-abcdefghijklmnop","status":"UNAVAILABLE"}}""";
        var handler = new SequenceHandler(Respond(HttpStatusCode.ServiceUnavailable, body));
        var (provider, _) = CreateGemini(handler);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.ProviderDetail.Should().NotContain(ApiKey).And.NotContain("sk-proj-abcdefghijklmnop").And.Contain("overloaded");
        health.Message.Should().NotContain(ApiKey);
    }

    [Fact]
    public async Task OpenAiAndAnthropic_CheckHealthAsync_503_AlsoRetryAndExtractDetail()
    {
        const string anthropicBody = """{"type":"error","error":{"type":"overloaded_error","message":"Overloaded"}}""";

        var openAiHandler = new SequenceHandler(
            Respond(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"The server is overloaded"}}"""),
            Respond(HttpStatusCode.OK, """{"choices":[{"message":{"content":"{\"ok\":true}"}}]}"""));
        var openAi = new OpenAiProvider(new FakeHttpClientFactory(openAiHandler), "sk-test-key-1234567890", "gpt-4.1-mini", delay: (_, _) => Task.CompletedTask);
        (await openAi.CheckHealthAsync(CancellationToken.None)).IsHealthy.Should().BeTrue();
        openAiHandler.CallCount.Should().Be(2);

        var anthropicHandler = new SequenceHandler(Respond((HttpStatusCode)529, anthropicBody));
        var anthropic = new AnthropicProvider(new FakeHttpClientFactory(anthropicHandler), "sk-ant-test-key-1234567890", "claude-sonnet-5", delay: (_, _) => Task.CompletedTask);
        var health = await anthropic.CheckHealthAsync(CancellationToken.None);
        health.StatusCode.Should().Be(529);
        health.ProviderDetail.Should().Be("Overloaded");
        anthropicHandler.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task CompleteJsonAsync_503_NoHttpLevelRetry()
    {
        var handler = new SequenceHandler(Respond(HttpStatusCode.ServiceUnavailable, OverloadedBody));
        var (provider, delays) = CreateGemini(handler);
        using var schema = JsonDocument.Parse(AnalysisJsonSchema.RawJson);

        var result = await provider.CompleteJsonAsync(new AiCompletionRequest("s", "u", schema, Model, 4096, 0.4), CancellationToken.None);

        result.ErrorKind.Should().Be(AiErrorKind.Server);
        handler.CallCount.Should().Be(1, "fon tahlilida retry `AnalysisOrchestrator`da (2s/6s/15s) — ikki qavat retry bo'lmasin");
        delays.Should().BeEmpty();
    }

    private sealed class TimeoutHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        private readonly TimeSpan _timeout;

        public TimeoutHttpClientFactory(HttpMessageHandler handler, TimeSpan timeout)
        {
            _handler = handler;
            _timeout = timeout;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false) { Timeout = _timeout };
    }
}

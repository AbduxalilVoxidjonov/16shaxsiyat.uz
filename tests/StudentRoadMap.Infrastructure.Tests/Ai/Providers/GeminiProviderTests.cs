using StudentRoadMap.Application.Common.Interfaces;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// `GeminiProvider` — tarmoqsiz (`FakeHttpMessageHandler`), `prompts/17` DoD: "har provider
/// uchun so'rov tanasi snapshot testi" + "xato xaritalash testlari". **Haqiqiy Gemini API
/// bilan sinalmagan — kalit yo'q** (`prompts/17` MUHIM eslatma).
/// </summary>
public sealed class GeminiProviderTests
{
    private const string ApiKey = "AIzaFAKEKEYFORTESTS1234567890";
    private const string Model = "gemini-2.0-flash";

    private static AiCompletionRequest BuildRequest()
    {
        var schema = JsonDocument.Parse(AnalysisJsonSchema.RawJson);
        return new AiCompletionRequest("tizim matni", "foydalanuvchi matni", schema, Model, 4096, 0.4);
    }

    private const string SuccessBody = """
        {
          "candidates": [
            { "content": { "parts": [ { "text": "{\"summary\":\"ok\"}" } ] }, "finishReason": "STOP" }
          ],
          "usageMetadata": { "promptTokenCount": 1900, "candidatesTokenCount": 2400 }
        }
        """;

    [Fact]
    public async Task CompleteJsonAsync_SuccessResponse_ReturnsRawJsonAndTokens()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorKind.Should().Be(AiErrorKind.None);
        result.RawJson.Should().Be("{\"summary\":\"ok\"}");
        result.InputTokens.Should().Be(1900);
        result.OutputTokens.Should().Be(2400);
    }

    [Fact]
    public async Task CompleteJsonAsync_SuccessResponse_RequestBodyMatchesGeminiStructuredOutputContract()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Be($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent");
        handler.LastRequest.RequestUri!.ToString().Should().NotContain(ApiKey, "kalit URL'da emas, sarlavhada bo'lishi kerak");
        handler.LastRequest.Headers.GetValues("x-goog-api-key").Should().ContainSingle().Which.Should().Be(ApiKey);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString().Should().Be("tizim matni");
        root.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString().Should().Be("foydalanuvchi matni");

        var generationConfig = root.GetProperty("generationConfig");
        generationConfig.GetProperty("responseMimeType").GetString().Should().Be("application/json");
        generationConfig.GetProperty("maxOutputTokens").GetInt32().Should().Be(4096);
        generationConfig.GetProperty("temperature").GetDouble().Should().Be(0.4);

        // Gemini `responseSchema` — `additionalProperties` qo'llab-quvvatlanmaydi (`GeminiSchemaAdapter`).
        generationConfig.GetProperty("responseSchema").TryGetProperty("additionalProperties", out _).Should().BeFalse();
        generationConfig.GetProperty("responseSchema").GetProperty("required").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompleteJsonAsync_401Unauthorized_MapsToAuthErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, """{"error":{"code":401,"message":"API key not valid"}}""");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Auth);
    }

    [Fact]
    public async Task CompleteJsonAsync_429TooManyRequests_MapsToRateLimitErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json((HttpStatusCode)429, """{"error":{"code":429,"message":"Resource exhausted"}}""");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.RateLimit);
    }

    [Fact]
    public async Task CompleteJsonAsync_500Server_MapsToServerErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.InternalServerError, """{"error":{"code":500,"message":"Internal error"}}""");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Server);
    }

    [Fact]
    public async Task CompleteJsonAsync_ClientTimeout_MapsToTimeoutErrorKind_NotCancellation()
    {
        var handler = FakeHttpMessageHandler.Timeout();
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Timeout);
    }

    [Fact]
    public async Task CompleteJsonAsync_CallerCancellation_PropagatesAsOperationCanceled_NotTimeout()
    {
        var handler = FakeHttpMessageHandler.Timeout();
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        // Chaqiruvchining o'zi bekor qilgan holat `Timeout` sifatida yashirilmasligi kerak —
        // fallback zanjiriga tushmaydi, to'g'ridan-to'g'ri yuqoriga tashlanadi.
        var act = async () => await provider.CompleteJsonAsync(BuildRequest(), new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CompleteJsonAsync_MalformedResponseBody_MapsToSchemaErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, "bu JSON emas, buzilgan javob <<<");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Schema);
        result.RawJson.Should().BeNull();
    }

    [Fact]
    public async Task CompleteJsonAsync_NoStructuredOutputReturned_MapsToSchemaErrorKind()
    {
        // 200 OK, lekin `finishReason: SAFETY` — model xavfsizlik siyosati sababli
        // hech qanday tarkib qaytarmadi ("structured output qaytmagan holat").
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"candidates":[{"finishReason":"SAFETY"}]}""");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Schema);
        result.ErrorMessage.Should().Contain("SAFETY");
    }

    [Fact]
    public async Task CompleteJsonAsync_ErrorBodyContainingApiKey_NeverLeaksKeyInErrorMessage()
    {
        // Adversarial ssenariy: provayder (haqiqatda bunday qilmaydi) xato tanasida kalitni
        // aks ettirsa ham — xato xabarida u ko'rinmasligi shart (`prompts/17` MAXSUS DIQQAT #1).
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, $"{{\"error\":{{\"message\":\"key {ApiKey} invalid\"}}}}");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.ErrorMessage.Should().NotContain(ApiKey);
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessResponse_ReturnsHealthy()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"candidates":[{"content":{"parts":[{"text":"{\"ok\":true}"}]}}]}""");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_401Unauthorized_ReturnsUnhealthyWithoutLeakingKey()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, """{"error":{"message":"API key not valid"}}""");
        var provider = new GeminiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeFalse();
        health.Message.Should().NotContain(ApiKey);
    }
}

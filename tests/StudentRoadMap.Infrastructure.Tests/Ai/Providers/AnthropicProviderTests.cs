using StudentRoadMap.Application.Common.Interfaces;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// `AnthropicProvider` — tarmoqsiz (`FakeHttpMessageHandler`), `prompts/17` DoD. **Haqiqiy
/// Anthropic API bilan sinalmagan — kalit yo'q** (`prompts/17` MUHIM eslatma).
/// </summary>
public sealed class AnthropicProviderTests
{
    private const string ApiKey = "sk-ant-FAKEKEYFORTESTS1234567890";
    private const string Model = "claude-sonnet-5";

    private static AiCompletionRequest BuildRequest()
    {
        var schema = JsonDocument.Parse(AnalysisJsonSchema.RawJson);
        return new AiCompletionRequest("tizim matni", "foydalanuvchi matni", schema, Model, 4096, 0.4);
    }

    private const string SuccessBody = """
        {
          "content": [ { "type": "tool_use", "name": "emit_analysis", "input": { "summary": "ok" } } ],
          "stop_reason": "tool_use",
          "usage": { "input_tokens": 2000, "output_tokens": 2500 }
        }
        """;

    [Fact]
    public async Task CompleteJsonAsync_SuccessResponse_ReturnsRawJsonAndTokens()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorKind.Should().Be(AiErrorKind.None);
        using var parsed = JsonDocument.Parse(result.RawJson!);
        parsed.RootElement.GetProperty("summary").GetString().Should().Be("ok");
        result.InputTokens.Should().Be(2000);
        result.OutputTokens.Should().Be(2500);
    }

    [Fact]
    public async Task CompleteJsonAsync_SuccessResponse_RequestBodyMatchesToolUseContract()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.anthropic.com/v1/messages");
        handler.LastRequest.Headers.GetValues("x-api-key").Should().ContainSingle().Which.Should().Be(ApiKey);
        handler.LastRequest.Headers.GetValues("anthropic-version").Should().ContainSingle().Which.Should().Be("2023-06-01");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().Should().Be(Model);
        root.GetProperty("system").GetString().Should().Be("tizim matni");
        root.GetProperty("messages")[0].GetProperty("role").GetString().Should().Be("user");
        root.GetProperty("messages")[0].GetProperty("content").GetString().Should().Be("foydalanuvchi matni");

        var tools = root.GetProperty("tools");
        tools.GetArrayLength().Should().Be(1);
        tools[0].GetProperty("name").GetString().Should().Be("emit_analysis");
        tools[0].GetProperty("input_schema").GetProperty("type").GetString().Should().Be("object");

        var toolChoice = root.GetProperty("tool_choice");
        toolChoice.GetProperty("type").GetString().Should().Be("tool");
        toolChoice.GetProperty("name").GetString().Should().Be("emit_analysis");
    }

    [Fact]
    public async Task CompleteJsonAsync_401Unauthorized_MapsToAuthErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, """{"error":{"type":"authentication_error","message":"invalid x-api-key"}}""");
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Auth);
    }

    [Fact]
    public async Task CompleteJsonAsync_429TooManyRequests_MapsToRateLimitErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json((HttpStatusCode)429, """{"error":{"type":"rate_limit_error","message":"rate limited"}}""");
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.RateLimit);
    }

    [Fact]
    public async Task CompleteJsonAsync_529Overloaded_MapsToServerErrorKind()
    {
        // Anthropic'ga xos "overloaded_error" — standart bo'lmagan 529 status kodi bilan qaytadi.
        var handler = FakeHttpMessageHandler.Json((HttpStatusCode)529, """{"error":{"type":"overloaded_error","message":"Overloaded"}}""");
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Server);
    }

    [Fact]
    public async Task CompleteJsonAsync_ClientTimeout_MapsToTimeoutErrorKind()
    {
        var handler = FakeHttpMessageHandler.Timeout();
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Timeout);
    }

    [Fact]
    public async Task CompleteJsonAsync_MalformedResponseBody_MapsToSchemaErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, "<<< buzilgan javob");
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Schema);
    }

    [Fact]
    public async Task CompleteJsonAsync_ToolNotInvoked_MapsToSchemaErrorKind()
    {
        // Model majburlangan toolni chaqirmadi — faqat oddiy matn bilan javob berdi.
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"content":[{"type":"text","text":"salom"}],"stop_reason":"end_turn"}""");
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Schema);
        result.ErrorMessage.Should().Contain("end_turn");
    }

    [Fact]
    public async Task CompleteJsonAsync_ErrorBodyContainingApiKey_NeverLeaksKeyInErrorMessage()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, $"{{\"error\":{{\"message\":\"key {ApiKey} invalid\"}}}}");
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.ErrorMessage.Should().NotContain(ApiKey);
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessResponse_ReturnsHealthy()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new AnthropicProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeTrue();
    }
}

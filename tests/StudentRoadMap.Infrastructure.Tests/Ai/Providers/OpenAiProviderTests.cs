using StudentRoadMap.Application.Common.Interfaces;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// `OpenAiProvider` — tarmoqsiz (`FakeHttpMessageHandler`), `prompts/17` DoD. **Haqiqiy OpenAI
/// API bilan sinalmagan — kalit yo'q** (`prompts/17` MUHIM eslatma).
/// </summary>
public sealed class OpenAiProviderTests
{
    private const string ApiKey = "sk-FAKEKEYFORTESTS1234567890";
    private const string Model = "gpt-4.1-mini";

    private static AiCompletionRequest BuildRequest()
    {
        var schema = JsonDocument.Parse(AnalysisJsonSchema.RawJson);
        return new AiCompletionRequest("tizim matni", "foydalanuvchi matni", schema, Model, 4096, 0.4);
    }

    private const string SuccessBody = """
        {
          "choices": [ { "message": { "role": "assistant", "content": "{\"summary\":\"ok\"}" } } ],
          "usage": { "prompt_tokens": 2100, "completion_tokens": 2600 }
        }
        """;

    [Fact]
    public async Task CompleteJsonAsync_SuccessResponse_ReturnsRawJsonAndTokens()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorKind.Should().Be(AiErrorKind.None);
        result.RawJson.Should().Be("{\"summary\":\"ok\"}");
        result.InputTokens.Should().Be(2100);
        result.OutputTokens.Should().Be(2600);
    }

    [Fact]
    public async Task CompleteJsonAsync_SuccessResponse_RequestBodyMatchesJsonSchemaStrictContract()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://api.openai.com/v1/chat/completions");
        handler.LastRequest.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization!.Parameter.Should().Be(ApiKey);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        root.GetProperty("model").GetString().Should().Be(Model);
        var messages = root.GetProperty("messages");
        messages[0].GetProperty("role").GetString().Should().Be("system");
        messages[0].GetProperty("content").GetString().Should().Be("tizim matni");
        messages[1].GetProperty("role").GetString().Should().Be("user");
        messages[1].GetProperty("content").GetString().Should().Be("foydalanuvchi matni");

        var responseFormat = root.GetProperty("response_format");
        responseFormat.GetProperty("type").GetString().Should().Be("json_schema");
        var jsonSchema = responseFormat.GetProperty("json_schema");
        jsonSchema.GetProperty("name").GetString().Should().Be("emit_analysis");
        jsonSchema.GetProperty("strict").GetBoolean().Should().BeTrue();
        jsonSchema.GetProperty("schema").GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public async Task CompleteJsonAsync_401Unauthorized_MapsToAuthErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, """{"error":{"message":"Incorrect API key provided"}}""");
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Auth);
    }

    [Fact]
    public async Task CompleteJsonAsync_429TooManyRequests_MapsToRateLimitErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json((HttpStatusCode)429, """{"error":{"message":"Rate limit reached"}}""");
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.RateLimit);
    }

    [Fact]
    public async Task CompleteJsonAsync_503Server_MapsToServerErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"Service unavailable"}}""");
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Server);
    }

    [Fact]
    public async Task CompleteJsonAsync_ClientTimeout_MapsToTimeoutErrorKind()
    {
        var handler = FakeHttpMessageHandler.Timeout();
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Timeout);
    }

    [Fact]
    public async Task CompleteJsonAsync_MalformedResponseBody_MapsToSchemaErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, "{ bu buzilgan JSON");
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Schema);
    }

    [Fact]
    public async Task CompleteJsonAsync_ModelRefusal_MapsToSchemaErrorKind()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","refusal":"I can't help with that."}}]}""");
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorKind.Should().Be(AiErrorKind.Schema);
    }

    [Fact]
    public async Task CompleteJsonAsync_ErrorBodyContainingApiKey_NeverLeaksKeyInErrorMessage()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.Unauthorized, $"{{\"error\":{{\"message\":\"key {ApiKey} invalid\"}}}}");
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var result = await provider.CompleteJsonAsync(BuildRequest(), CancellationToken.None);

        result.ErrorMessage.Should().NotContain(ApiKey);
    }

    [Fact]
    public async Task CheckHealthAsync_SuccessResponse_ReturnsHealthy()
    {
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.OK, SuccessBody);
        var provider = new OpenAiProvider(new FakeHttpClientFactory(handler), ApiKey, Model);

        var health = await provider.CheckHealthAsync(CancellationToken.None);

        health.IsHealthy.Should().BeTrue();
    }
}

using StudentRoadMap.Application.Common.Interfaces;
using System.Net;
using FluentAssertions;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Infrastructure.Ai.Providers;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>Uchala providerga umumiy status-kod xaritalash (`prompts/17` vazifa #1) va kalitni tozalash (MAXSUS DIQQAT #1) qoidalarini to'g'ridan-to'g'ri qamrab oladi.</summary>
public sealed class AiHttpExecutorTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, AiErrorKind.Auth)]
    [InlineData(HttpStatusCode.Forbidden, AiErrorKind.Auth)]
    [InlineData((HttpStatusCode)429, AiErrorKind.RateLimit)]
    [InlineData(HttpStatusCode.InternalServerError, AiErrorKind.Server)]
    [InlineData(HttpStatusCode.ServiceUnavailable, AiErrorKind.Server)]
    [InlineData((HttpStatusCode)529, AiErrorKind.Server)]
    // 400/404/422 va boshqa 4xx (401/403/429 dan tashqari) → `BadRequest` — PM ko'rsatmasi
    // (2026-09-02): so'rov shaklining o'zi rad etildi, shu providerda qayta urinishga
    // arzimaydi, lekin fallback zanjiridagi keyingi providerga o'tiladi (P18).
    [InlineData(HttpStatusCode.BadRequest, AiErrorKind.BadRequest)]
    [InlineData(HttpStatusCode.NotFound, AiErrorKind.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity, AiErrorKind.BadRequest)]
    public void ClassifyStatusCode_MapsAccordingToPromptsSeventeenRule(HttpStatusCode statusCode, AiErrorKind expected)
    {
        AiHttpExecutor.ClassifyStatusCode(statusCode).Should().Be(expected);
    }

    [Fact]
    public void Redact_ReplacesApiKeyOccurrenceWithPlaceholder()
    {
        var text = AiHttpExecutor.Redact("xato: kalit AIzaSECRET1234 noto'g'ri", "AIzaSECRET1234");

        text.Should().NotContain("AIzaSECRET1234");
        text.Should().Contain("***");
    }

    [Fact]
    public void Redact_EmptyApiKey_ReturnsTextUnchanged()
    {
        const string text = "xato xabari";

        AiHttpExecutor.Redact(text, string.Empty).Should().Be(text);
    }

    [Fact]
    public async Task SendAsync_HttpRequestException_MapsToNetworkAndRedactsKey()
    {
        var handler = new ThrowingHandler(new HttpRequestException("DNS xatosi: AIzaSECRET1234"));
        using var httpClient = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid");

        var outcome = await AiHttpExecutor.SendAsync(httpClient, request, "AIzaSECRET1234", CancellationToken.None);

        outcome.Success.Should().BeFalse();
        outcome.ErrorKind.Should().Be(AiErrorKind.Network);
        outcome.ErrorMessage.Should().NotContain("AIzaSECRET1234");
    }

    /// <summary>
    /// P28 jonli tekshiruvda (2026-09-02) topilgan xato: Gemini noto'g'ri kalitga `400` +
    /// `API_KEY_INVALID` qaytaradi, faqat status kodiga tayangan xaritalash uni `BadRequest`
    /// ("bu bizning xatomiz") deb ko'rsatardi — admin uchun mutlaqo chalg'ituvchi. Endi tana
    /// matni ham hisobga olinadi (tana FAQAT tur aniqlash uchun, xabarga TUSHMAYDI).
    /// </summary>
    [Theory]
    // Gemini — noto'g'ri kalit 400 bilan keladi.
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"code\":400,\"status\":\"INVALID_ARGUMENT\",\"details\":[{\"reason\":\"API_KEY_INVALID\"}]}}", AiErrorKind.Auth)]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"message\":\"API key not valid. Please pass a valid API key.\"}}", AiErrorKind.Auth)]
    // OpenAI — 401 invalid_api_key.
    [InlineData(HttpStatusCode.Unauthorized, "{\"error\":{\"code\":\"invalid_api_key\"}}", AiErrorKind.Auth)]
    // Anthropic — 401 authentication_error.
    [InlineData(HttpStatusCode.Unauthorized, "{\"type\":\"error\",\"error\":{\"type\":\"authentication_error\"}}", AiErrorKind.Auth)]
    // Model nomi xato.
    [InlineData(HttpStatusCode.NotFound, "{\"error\":{\"message\":\"models/yoq-model is not found for API version v1beta\"}}", AiErrorKind.ModelNotFound)]
    [InlineData(HttpStatusCode.NotFound, "{\"error\":{\"code\":\"model_not_found\"}}", AiErrorKind.ModelNotFound)]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"code\":\"model_not_found\"}}", AiErrorKind.ModelNotFound)]
    // Limit va server xatolari tana matnidan qat'i nazar o'z turida qoladi.
    [InlineData((HttpStatusCode)429, "{\"error\":{\"message\":\"quota exceeded, model_not_found\"}}", AiErrorKind.RateLimit)]
    [InlineData(HttpStatusCode.InternalServerError, "{\"error\":{\"message\":\"API key not valid\"}}", AiErrorKind.Server)]
    // Tanada hech qanday belgi yo'q — status kodi bo'yicha.
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"message\":\"invalid field 'responseSchema'\"}}", AiErrorKind.BadRequest)]
    public void ClassifyFailure_UsesResponseBodyToRefineStatusCodeMapping(HttpStatusCode statusCode, string body, AiErrorKind expected)
    {
        AiHttpExecutor.ClassifyFailure(statusCode, body).Should().Be(expected);
    }

    [Fact]
    public async Task SendAsync_GeminiInvalidApiKeyBody_ClassifiedAsAuthNotBadRequest()
    {
        const string body = "{\"error\":{\"code\":400,\"message\":\"API key not valid. Please pass a valid API key.\",\"status\":\"INVALID_ARGUMENT\"}}";
        var handler = FakeHttpMessageHandler.Json(HttpStatusCode.BadRequest, body);
        using var httpClient = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.invalid");

        var outcome = await AiHttpExecutor.SendAsync(httpClient, request, "AIzaSECRET1234", CancellationToken.None);

        outcome.ErrorKind.Should().Be(AiErrorKind.Auth);
    }

    [Theory]
    [InlineData("{\"error\":{\"code\":503,\"message\":\"The model is overloaded. Please try again later.\",\"status\":\"UNAVAILABLE\"}}", "The model is overloaded. Please try again later.")]
    [InlineData("{\"type\":\"error\",\"error\":{\"type\":\"overloaded_error\",\"message\":\"Overloaded\"}}", "Overloaded")]
    [InlineData("[{\"error\":{\"message\":\"array wrapped\"}}]", "array wrapped")]
    [InlineData("<html>502 Bad Gateway</html>", null)]
    [InlineData("{\"error\":\"just a string\"}", null)]
    [InlineData("", null)]
    public void ExtractSafeDetail_ReadsOnlyErrorMessageField(string body, string? expected)
    {
        AiHttpExecutor.ExtractSafeDetail(body, "AIzaSECRET1234").Should().Be(expected);
    }

    [Fact]
    public void ExtractSafeDetail_RedactsKeyAndTruncates()
    {
        var longText = new string('x', 500);
        var body = $"{{\"error\":{{\"message\":\"key AIzaSECRET1234 bad {longText}\"}}}}";

        var detail = AiHttpExecutor.ExtractSafeDetail(body, "AIzaSECRET1234");

        detail.Should().NotContain("AIzaSECRET1234");
        detail!.Length.Should().BeLessThanOrEqualTo(201);
    }

    [Theory]
    [InlineData(503, null, 1.0)]
    [InlineData(502, null, 1.0)]
    [InlineData(504, null, 1.0)]
    [InlineData(529, null, 1.0)]
    [InlineData(503, 4.0, 4.0)]
    [InlineData(503, 30.0, null)]
    [InlineData(429, null, null)]
    [InlineData(429, 2.0, 2.0)]
    [InlineData(429, 10.0, null)]
    [InlineData(500, null, null)]
    [InlineData(404, null, null)]
    public void GetRetryDelay_OnlyTransientStatusesWithBoundedRetryAfter(int status, double? retryAfterSeconds, double? expectedSeconds)
    {
        var outcome = new AiHttpOutcome(false, string.Empty, AiErrorKind.Server, "x", status, null,
            retryAfterSeconds is { } ra ? TimeSpan.FromSeconds(ra) : null);

        var delay = AiHttpExecutor.GetRetryDelay(outcome, TimeSpan.FromSeconds(1));

        delay.Should().Be(expectedSeconds is { } e ? TimeSpan.FromSeconds(e) : null);
    }

    [Fact]
    public void GetRetryDelay_NoStatusCode_Timeout_NoRetry()
    {
        var outcome = new AiHttpOutcome(false, string.Empty, AiErrorKind.Timeout, "timeout");

        AiHttpExecutor.GetRetryDelay(outcome, TimeSpan.FromSeconds(1)).Should().BeNull();
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw _exception;
    }
}

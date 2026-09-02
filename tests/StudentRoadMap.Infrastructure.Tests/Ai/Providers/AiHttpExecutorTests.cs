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
    public async Task SendAsync_HttpRequestException_MapsToUnknownAndRedactsKey()
    {
        var handler = new ThrowingHandler(new HttpRequestException("DNS xatosi: AIzaSECRET1234"));
        using var httpClient = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid");

        var outcome = await AiHttpExecutor.SendAsync(httpClient, request, "AIzaSECRET1234", CancellationToken.None);

        outcome.Success.Should().BeFalse();
        outcome.ErrorKind.Should().Be(AiErrorKind.Unknown);
        outcome.ErrorMessage.Should().NotContain("AIzaSECRET1234");
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw _exception;
    }
}

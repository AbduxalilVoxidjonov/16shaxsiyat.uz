using System.Net;

namespace StudentRoadMap.Infrastructure.Tests.Ai.Providers;

/// <summary>
/// Tarmoqqa chiqmaydigan soxta `HttpMessageHandler` — provider testlari uchun (`prompts/17`
/// MUHIM eslatma: "sinovni soxta HTTP handler bilan qil — haqiqiy tarmoqqa chiqma"). So'nggi
/// yuborilgan so'rov va uning tanasi keyingi tekshiruv (snapshot) uchun saqlanadi.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Simulyatsiya qilingan timeout — `HttpClient` o'zining ichki taymeri tugaganda
    /// tashlaydigan `TaskCanceledException`ga o'xshash (`InnerException` — `TimeoutException`,
    /// chaqiruvchi `CancellationToken`ning o'zi bekor qilinmagan).</summary>
    public static FakeHttpMessageHandler Timeout() =>
        new(_ => throw new TaskCanceledException("Simulated HttpClient timeout.", new TimeoutException()));

    public static FakeHttpMessageHandler Json(HttpStatusCode statusCode, string json) =>
        new(_ => new HttpResponseMessage(statusCode) { Content = new StringContent(json) });

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return _responder(request);
    }
}

/// <summary>`IHttpClientFactory` ning testga xos, doim bitta soxta handler qaytaruvchi ko'rinishi.</summary>
internal sealed class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public FakeHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name) => new(_handler);
}

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// Google Gemini — structured output `generationConfig.responseMimeType = "application/json"`
/// + `generationConfig.responseSchema` orqali (`docs/09-ai-analiz-moduli.md` 2-bo'lim jadvali,
/// `prompts/17` vazifa #1). API kaliti so'rov tanasida yoki URL query-parametrida EMAS —
/// `x-goog-api-key` sarlavhasida yuboriladi (Google buni qo'llab-quvvatlaydi), shu bilan kalit
/// hech qachon so'rov URL'ida (demak — HTTP jurnallarida) ko'rinmaydi.
/// **Haqiqiy provider bilan sinalmadi — kalit yo'q** (`prompts/17` MUHIM eslatma).
/// </summary>
public sealed class GeminiProvider : IAiAnalysisProvider
{
    public const string HttpClientName = "AiProvider:Gemini";

    private const string DefaultBaseUrl = "https://generativelanguage.googleapis.com";

    private const string HealthCheckSchemaJson = """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"],"additionalProperties":false}""";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public GeminiProvider(IHttpClientFactory httpClientFactory, string apiKey, string model, string? baseUrl = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API kaliti bo'sh bo'lishi mumkin emas.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model nomi bo'sh bo'lishi mumkin emas.", nameof(model));
        }

        _apiKey = apiKey;
        _model = model;
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
    }

    public AiProvider Kind => AiProvider.Gemini;

    public async Task<AiCompletionResult> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        using var httpRequest = BuildRequest(request.SystemText, request.UserText, request.JsonSchema.RootElement, request.MaxOutputTokens, request.Temperature);
        var outcome = await AiHttpExecutor.SendAsync(httpClient, httpRequest, _apiKey, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        if (!outcome.Success)
        {
            return new AiCompletionResult(false, null, null, null, (int)stopwatch.ElapsedMilliseconds, outcome.ErrorMessage, outcome.ErrorKind);
        }

        return ParseCompletion(outcome.Body, (int)stopwatch.ElapsedMilliseconds);
    }

    public async Task<AiHealthResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var schemaDocument = JsonDocument.Parse(HealthCheckSchemaJson);

        using var httpRequest = BuildRequest(
            systemText: "Sen ulanishni tekshirish uchun chaqirilding.",
            userText: "Faqat {\"ok\": true} qaytar.",
            schema: schemaDocument.RootElement,
            maxOutputTokens: 32,
            temperature: 0);

        var stopwatch = Stopwatch.StartNew();
        var outcome = await AiHttpExecutor.SendAsync(httpClient, httpRequest, _apiKey, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return outcome.Success
            ? new AiHealthResult(true, $"Gemini ulanishi muvaffaqiyatli ({stopwatch.ElapsedMilliseconds} ms).")
            : new AiHealthResult(false, outcome.ErrorMessage, outcome.ErrorKind);
    }

    private HttpRequestMessage BuildRequest(string systemText, string userText, JsonElement schema, int maxOutputTokens, double temperature)
    {
        var body = new JsonObject
        {
            ["systemInstruction"] = new JsonObject
            {
                ["parts"] = new JsonArray(new JsonObject { ["text"] = systemText }),
            },
            ["contents"] = new JsonArray(
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray(new JsonObject { ["text"] = userText }),
                }),
            ["generationConfig"] = new JsonObject
            {
                ["responseMimeType"] = "application/json",
                ["responseSchema"] = GeminiSchemaAdapter.Convert(schema),
                ["maxOutputTokens"] = maxOutputTokens,
                ["temperature"] = temperature,
            },
        };

        var uri = $"{_baseUrl}/v1beta/models/{Uri.EscapeDataString(_model)}:generateContent";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("x-goog-api-key", _apiKey);
        return httpRequest;
    }

    private static AiCompletionResult ParseCompletion(string body, int durationMs)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var candidate = root.GetProperty("candidates").EnumerateArray().First();

            // Gemini bloklangan/xavfsizlik sababli to'xtatilgan javobda `content` bo'lmasligi
            // mumkin — bu "structured output qaytmagan holat" (`prompts/17` MUHIM eslatma).
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "noma'lum";
                return SchemaFailure($"Gemini structured output qaytarmadi (finishReason: {finishReason}).", durationMs);
            }

            var text = parts.EnumerateArray().First().GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return SchemaFailure("Gemini javobida matn bo'sh.", durationMs);
            }

            int? inputTokens = null;
            int? outputTokens = null;
            if (root.TryGetProperty("usageMetadata", out var usage))
            {
                inputTokens = usage.TryGetProperty("promptTokenCount", out var pt) ? pt.GetInt32() : null;
                outputTokens = usage.TryGetProperty("candidatesTokenCount", out var ct) ? ct.GetInt32() : null;
            }

            return new AiCompletionResult(true, text, inputTokens, outputTokens, durationMs, null, AiErrorKind.None);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return SchemaFailure($"Gemini javobini ajratishda xato (buzilgan JSON): {ex.Message}", durationMs);
        }
    }

    private static AiCompletionResult SchemaFailure(string message, int durationMs) =>
        new(false, null, null, null, durationMs, message, AiErrorKind.Schema);
}

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// OpenAI — structured output `response_format: { type: "json_schema", json_schema: { strict:
/// true } }` orqali, Chat Completions API (`POST /v1/chat/completions`) — bu API `docs/09`
/// 2-bo'limidagi "eng qat'iy schema qo'llab-quvvatlash" iborasiga mos, barqaror va keng
/// hujjatlashtirilgan (yangiroq Responses API o'rniga tanlandi). Kalit `Authorization: Bearer`
/// sarlavhasida. **Haqiqiy provider bilan sinalmadi — kalit yo'q** (`prompts/17` MUHIM eslatma).
///
/// `strict: true` rejimi har bir ob'ekt darajasida BARCHA `properties` kalitlarining `required`
/// massivida bo'lishini talab qiladi (ichma-ich ob'ektlar ham) — kanonik `AnalysisJsonSchema`
/// (P16, `docs/09` 5-bo'lim, validatorning yagona haqiqat manbai, bu yerda O'ZGARTIRILMAYDI)
/// buni to'liq qondirmaydi (masalan `careerSuggestions[].exampleProfessions` ixtiyoriy). PM
/// qarori (`prompts/17` javob xati, 2026-09-02): moslashtirish PROVIDER tomonda — so'rovga
/// jo'natishdan oldin `OpenAiSchemaAdapter.ToStrictSchema` ixtiyoriy maydonlarni `required`ga
/// qo'shib, `type`iga `"null"` qo'shadi; javobni qabul qilgach `OpenAiResponseNormalizer`
/// aynan shu (kanonik sxemaga nisbatan ixtiyoriy) `null` qiymatli kalitlarni olib tashlaydi —
/// natijada `RawJson` kanonik sxemaga (`AiResponseValidator`) to'g'ridan-to'g'ri mos keladi.
/// </summary>
public sealed class OpenAiProvider : IAiAnalysisProvider
{
    public const string HttpClientName = "AiProvider:OpenAi";

    private const string DefaultBaseUrl = "https://api.openai.com";

    private const string HealthCheckSchemaJson = """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"],"additionalProperties":false}""";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public OpenAiProvider(IHttpClientFactory httpClientFactory, string apiKey, string model, string? baseUrl = null)
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

    public AiProvider Kind => AiProvider.OpenAi;

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

        return ParseCompletion(outcome.Body, request.JsonSchema.RootElement, (int)stopwatch.ElapsedMilliseconds);
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
            ? new AiHealthResult(true, $"OpenAI ulanishi muvaffaqiyatli ({stopwatch.ElapsedMilliseconds} ms).")
            : new AiHealthResult(false, outcome.ErrorMessage, outcome.ErrorKind);
    }

    private HttpRequestMessage BuildRequest(string systemText, string userText, JsonElement schema, int maxOutputTokens, double temperature)
    {
        var schemaNode = OpenAiSchemaAdapter.ToStrictSchema(schema);

        var body = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = systemText },
                new JsonObject { ["role"] = "user", ["content"] = userText }),
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = "emit_analysis",
                    ["schema"] = schemaNode,
                    ["strict"] = true,
                },
            },
            ["max_tokens"] = maxOutputTokens,
            ["temperature"] = temperature,
        };

        var uri = $"{_baseUrl}/v1/chat/completions";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return httpRequest;
    }

    private static AiCompletionResult ParseCompletion(string body, JsonElement canonicalSchema, int durationMs)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var message = root.GetProperty("choices").EnumerateArray().First().GetProperty("message");

            // Model xavfsizlik/siyosat sababli rad etsa, `content` o'rniga `refusal` maydoni
            // to'ldiriladi — bu ham "structured output qaytmagan holat" (`prompts/17` MUHIM eslatma).
            if (message.TryGetProperty("refusal", out var refusal) && refusal.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            {
                return SchemaFailure($"OpenAI structured output'ni rad etdi: {refusal.GetString()}", durationMs);
            }

            if (!message.TryGetProperty("content", out var contentElement) || contentElement.ValueKind != JsonValueKind.String)
            {
                return SchemaFailure("OpenAI javobida 'content' maydoni topilmadi.", durationMs);
            }

            var content = contentElement.GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                return SchemaFailure("OpenAI javobi bo'sh.", durationMs);
            }

            // `strict` javobida ixtiyoriy (kanonik sxemada `required` bo'lmagan) maydonlar
            // to'ldirilmagan bo'lsa `null` bo'lib keladi — kanonik sxema ularni qabul qilmaydi,
            // shuning uchun validatorga uzatishdan oldin olib tashlanadi (`OpenAiSchemaAdapter`
            // hujjati, PM ko'rsatmasi 2026-09-02).
            var normalizedContent = OpenAiResponseNormalizer.Normalize(content, canonicalSchema);

            int? inputTokens = null;
            int? outputTokens = null;
            if (root.TryGetProperty("usage", out var usage))
            {
                inputTokens = usage.TryGetProperty("prompt_tokens", out var pt) ? pt.GetInt32() : null;
                outputTokens = usage.TryGetProperty("completion_tokens", out var ct) ? ct.GetInt32() : null;
            }

            return new AiCompletionResult(true, normalizedContent, inputTokens, outputTokens, durationMs, null, AiErrorKind.None);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return SchemaFailure($"OpenAI javobini ajratishda xato (buzilgan JSON): {ex.Message}", durationMs);
        }
    }

    private static AiCompletionResult SchemaFailure(string message, int durationMs) =>
        new(false, null, null, null, durationMs, message, AiErrorKind.Schema);
}

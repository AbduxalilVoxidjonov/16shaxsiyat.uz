using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// Anthropic Claude — structured output tool-use orqali: bitta majburiy `emit_analysis` tool
/// e'lon qilinadi (`input_schema` = `AnalysisJsonSchema`) va `tool_choice` shu toolni majburlaydi
/// — model erkin matn emas, aynan shu tool chaqiruvini qaytaradi (`docs/09-ai-analiz-moduli.md`
/// 2-bo'lim jadvali: "tool-use ('emit_analysis' tool, input_schema)"). Kalit `x-api-key`
/// sarlavhasida, `anthropic-version` sarlavhasi majburiy. **Haqiqiy provider bilan
/// sinalmadi — kalit yo'q** (`prompts/17` MUHIM eslatma).
/// </summary>
public sealed class AnthropicProvider : IAiAnalysisProvider
{
    public const string HttpClientName = "AiProvider:Anthropic";

    private const string DefaultBaseUrl = "https://api.anthropic.com";
    private const string AnthropicVersion = "2023-06-01";
    private const string ToolName = "emit_analysis";

    private const string HealthCheckSchemaJson = """{"type":"object","properties":{"ok":{"type":"boolean"}},"required":["ok"],"additionalProperties":false}""";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    // `delay` — "Aloqani tekshirish" qayta urinishlari orasidagi kutish; faqat testlar almashtiradi (standart `Task.Delay`).
    public AnthropicProvider(IHttpClientFactory httpClientFactory, string apiKey, string model, string? baseUrl = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
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
        _delay = delay ?? Task.Delay;
    }

    public AiProvider Kind => AiProvider.Anthropic;

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

        // Vaqtinchalik xatolarda (503/429 Retry-After) qisqa backoff bilan 2 ta qayta urinish —
        // FAQAT shu yerda; fon tahlilida `AnalysisOrchestrator`ning o'z retry'i bor (`docs/09` 7-bo'lim).
        var stopwatch = Stopwatch.StartNew();
        var outcome = await AiHttpExecutor.SendWithTransientRetryAsync(
            httpClient,
            () => BuildRequest(
                systemText: "Sen ulanishni tekshirish uchun chaqirilding.",
                userText: "Faqat {\"ok\": true} qaytar.",
                schema: schemaDocument.RootElement,
                maxOutputTokens: 32,
                temperature: 0),
            _apiKey,
            _delay,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        return outcome.Success
            ? new AiHealthResult(true, $"Anthropic ulanishi muvaffaqiyatli ({stopwatch.ElapsedMilliseconds} ms).")
            : new AiHealthResult(false, outcome.ErrorMessage, outcome.ErrorKind, outcome.StatusCode, outcome.ProviderDetail, _model);
    }

    private HttpRequestMessage BuildRequest(string systemText, string userText, JsonElement schema, int maxOutputTokens, double temperature)
    {
        var schemaNode = JsonNode.Parse(schema.GetRawText());

        var body = new JsonObject
        {
            ["model"] = _model,
            ["max_tokens"] = maxOutputTokens,
            ["temperature"] = temperature,
            ["system"] = systemText,
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = userText }),
            ["tools"] = new JsonArray(
                new JsonObject
                {
                    ["name"] = ToolName,
                    ["description"] = "O'quvchining test natijalari asosidagi to'liq tuzilgan tahlilni qaytaradi.",
                    ["input_schema"] = schemaNode,
                }),
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = ToolName },
        };

        var uri = $"{_baseUrl}/v1/messages";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
        return httpRequest;
    }

    private static AiCompletionResult ParseCompletion(string body, int durationMs)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var toolUseBlock = root.GetProperty("content").EnumerateArray()
                .FirstOrDefault(block =>
                    block.TryGetProperty("type", out var type) && type.GetString() == "tool_use" &&
                    block.TryGetProperty("name", out var name) && name.GetString() == ToolName);

            // Topilmasa (default(JsonElement)) `ValueKind == Undefined` — model majburlangan
            // toolni chaqirmadi (masalan `stop_reason: "end_turn"` matn bilan) — "structured
            // output qaytmagan holat" (`prompts/17` MUHIM eslatma).
            if (toolUseBlock.ValueKind is JsonValueKind.Undefined)
            {
                var stopReason = root.TryGetProperty("stop_reason", out var sr) ? sr.GetString() : "noma'lum";
                return SchemaFailure($"Anthropic '{ToolName}' tool chaqiruvini qaytarmadi (stop_reason: {stopReason}).", durationMs);
            }

            var input = toolUseBlock.GetProperty("input");
            var rawJson = input.GetRawText();

            int? inputTokens = null;
            int? outputTokens = null;
            if (root.TryGetProperty("usage", out var usage))
            {
                inputTokens = usage.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : null;
                outputTokens = usage.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : null;
            }

            return new AiCompletionResult(true, rawJson, inputTokens, outputTokens, durationMs, null, AiErrorKind.None);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return SchemaFailure($"Anthropic javobini ajratishda xato (buzilgan JSON): {ex.Message}", durationMs);
        }
    }

    private static AiCompletionResult SchemaFailure(string message, int durationMs) =>
        new(false, null, null, null, durationMs, message, AiErrorKind.Schema);
}

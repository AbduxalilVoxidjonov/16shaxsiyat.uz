using System.Diagnostics;
using System.Net;
using System.Text.Json;
using StudentRoadMap.Application.Ai;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// Provayder javobi haqidagi xom natija — `Body` faqat muvaffaqiyatli (`Success = true`)
/// holatda ma'noli, aks holda `ErrorMessage` (kalitsiz, `docs/09` va `prompts/17` MAXSUS
/// DIQQAT #1) ishlatiladi.
/// <para>
/// `StatusCode` — HTTP javob bo'lsa uning kodi (timeout/tarmoq xatosida `null`);
/// `ProviderDetail` — javob tanasidagi `error.message` (kalitsiz, qisqartirilgan —
/// `ExtractSafeDetail`); `RetryAfter` — `Retry-After` sarlavhasi (bo'lsa).
/// </para>
/// </summary>
internal sealed record AiHttpOutcome(
    bool Success,
    string Body,
    AiErrorKind ErrorKind,
    string? ErrorMessage,
    int? StatusCode = null,
    string? ProviderDetail = null,
    TimeSpan? RetryAfter = null);

/// <summary>
/// Uchala provayder (`GeminiProvider`/`OpenAiProvider`/`AnthropicProvider`) uchun umumiy HTTP
/// yuborish, xatoni `AiErrorKind`ga xaritalash va API kalitini xato xabaridan tozalash mantig'i
/// (`prompts/17` vazifa #1: "xatoni AiErrorKind ga xaritalash (401/403 → Auth, 429 → RateLimit,
/// 5xx → Server, timeout → Timeout)" va MAXSUS DIQQAT #1: "kalit hech qachon ... xato xabariga
/// ... tushmasin").
/// </summary>
internal static class AiHttpExecutor
{
    /// <summary>Xato xabariga qo'shiladigan provayder tanasining maksimal uzunligi (log shovqinini cheklash uchun).</summary>
    private const int MaxErrorBodyLength = 300;

    /// <summary>
    /// "Aloqani tekshirish" uchun vaqtinchalik xatolardan keyingi kutish vaqtlari — 2 ta qayta
    /// urinish (1 s, 3 s). FAQAT `CheckHealthAsync`da ishlatiladi: fon tahlilida
    /// (`AnalysisOrchestrator`) o'zining 2 s / 6 s / 15 s retry'i bor — HTTP darajasida yana
    /// retry qo'shilsa urinishlar ko'paytiriladi (3 × 3), xarajat va yuklama oshadi.
    /// </summary>
    internal static readonly TimeSpan[] HealthCheckRetryBackoffs = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    /// <summary>`Retry-After` shundan uzun bo'lsa qayta urinilmaydi (admin ekrani uzoq kutib qolmasin).</summary>
    internal static readonly TimeSpan MaxHonoredRetryAfter = TimeSpan.FromSeconds(5);

    /// <summary>`HttpClient.Timeout` cheksiz bo'lsa umumiy vaqt byudjeti (`docs/09` 7-bo'lim, 90 s).</summary>
    private static readonly TimeSpan DefaultBudget = TimeSpan.FromSeconds(90);

    private const string TimeoutMessage = "So'rov belgilangan vaqt ichida javob bermadi (timeout).";

    /// <summary>Noto'g'ri/bekor qilingan kalitni bildiruvchi javob belgilari (Gemini/OpenAI/Anthropic).</summary>
    private static readonly string[] InvalidKeyMarkers =
    [
        "API_KEY_INVALID",
        "API key not valid",
        "invalid_api_key",
        "invalid x-api-key",
        "authentication_error",
        "PERMISSION_DENIED",
    ];

    /// <summary>Model nomi topilmaganini bildiruvchi javob belgilari.</summary>
    private static readonly string[] ModelNotFoundMarkers =
    [
        "model_not_found",
        "is not found for API version",
        "not_found_error",
        "does not exist or you do not have access",
    ];

    public static async Task<AiHttpOutcome> SendAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        string apiKey,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // `HttpClient.Timeout` ichki taymeri tugadi — chaqiruvchi o'zi bekor qilmadi
            // (`cancellationToken.IsCancellationRequested == false`). Chaqiruvchi bekor
            // qilgan holat esa QAYTA ULANMAYDI (yuqoriga tashlanadi) — bu haqiqiy bekor
            // qilish, fallback zanjiriga arzimaydi.
            return new AiHttpOutcome(false, string.Empty, AiErrorKind.Timeout, TimeoutMessage);
        }
        catch (HttpRequestException ex)
        {
            return new AiHttpOutcome(false, string.Empty, AiErrorKind.Network, Redact($"Tarmoqqa ulanishda xato: {ex.Message}", apiKey));
        }

        using (response)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new AiHttpOutcome(false, string.Empty, AiErrorKind.Timeout, TimeoutMessage);
            }

            if (response.IsSuccessStatusCode)
            {
                return new AiHttpOutcome(true, body, AiErrorKind.None, null);
            }

            var errorKind = ClassifyFailure(response.StatusCode, body);
            var message = Redact(BuildErrorMessage(response.StatusCode, body), apiKey);
            return new AiHttpOutcome(
                false,
                body,
                errorKind,
                message,
                (int)response.StatusCode,
                ExtractSafeDetail(body, apiKey),
                ReadRetryAfter(response));
        }
    }

    /// <summary>
    /// "Aloqani tekshirish" uchun: vaqtinchalik xatoda (`IsTransient`) qisqa backoff bilan
    /// ko'pi bilan 2 marta qayta yuboradi. `HttpRequestMessage` qayta yuborilmaydi — har urinish
    /// uchun `requestFactory` yangisini quradi. Umumiy vaqt (barcha urinishlar + kutishlar)
    /// `HttpClient.Timeout` dan oshmaydi: byudjet tugasa oxirgi natija / timeout qaytariladi.
    /// </summary>
    public static async Task<AiHttpOutcome> SendWithTransientRetryAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> requestFactory,
        string apiKey,
        Func<TimeSpan, CancellationToken, Task> delay,
        CancellationToken cancellationToken)
    {
        var budget = httpClient.Timeout == Timeout.InfiniteTimeSpan ? DefaultBudget : httpClient.Timeout;
        var stopwatch = Stopwatch.StartNew();
        using var budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budgetCts.CancelAfter(budget);

        for (var attempt = 0; ; attempt++)
        {
            AiHttpOutcome outcome;
            try
            {
                using var request = requestFactory();
                outcome = await SendAsync(httpClient, request, apiKey, budgetCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Umumiy byudjet tugadi (chaqiruvchi bekor qilmagan).
                return new AiHttpOutcome(false, string.Empty, AiErrorKind.Timeout, TimeoutMessage);
            }

            if (outcome.Success || attempt >= HealthCheckRetryBackoffs.Length)
            {
                return outcome;
            }

            var wait = GetRetryDelay(outcome, HealthCheckRetryBackoffs[attempt]);
            if (wait is null || stopwatch.Elapsed + wait.Value >= budget)
            {
                return outcome;
            }

            try
            {
                await delay(wait.Value, budgetCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return outcome;
            }
        }
    }

    /// <summary>
    /// Qayta urinishdan oldin kutish vaqti, yoki `null` — qayta urinilmaydi.
    /// 502/503/504/529 (vaqtinchalik yuklama) → `Retry-After` (≤ 5 s) yoki standart backoff;
    /// 429 → FAQAT `Retry-After` bo'lsa va ≤ 5 s (sarlavhasiz 429 odatda kvota tugagani —
    /// qayta urinish befoyda); `Retry-After` > 5 s → qayta urinilmaydi.
    /// </summary>
    internal static TimeSpan? GetRetryDelay(AiHttpOutcome outcome, TimeSpan defaultBackoff)
    {
        if (outcome.StatusCode is not { } status)
        {
            return null;
        }

        var retryAfter = outcome.RetryAfter;
        if (retryAfter is { } ra && ra > MaxHonoredRetryAfter)
        {
            return null;
        }

        return status switch
        {
            429 => retryAfter,
            502 or 503 or 504 or 529 => retryAfter ?? defaultBackoff,
            _ => null,
        };
    }

    /// <summary>
    /// Provayder javobidagi `error.message` (Gemini/OpenAI/Anthropic uchalasida shu joyda) —
    /// aniq kalit `Redact` bilan, kalitga o'xshash satrlar `AiErrorDetailSanitizer` bilan
    /// tozalanadi va 200 belgigacha qisqartiriladi. JSON bo'lmasa yoki maydon yo'q bo'lsa `null`
    /// (xom tana HECH QACHON qaytarilmaydi).
    /// </summary>
    internal static string? ExtractSafeDetail(string body, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                root = root[0];
            }

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("error", out var error) ||
                error.ValueKind != JsonValueKind.Object ||
                !error.TryGetProperty("message", out var messageElement) ||
                messageElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return AiErrorDetailSanitizer.Sanitize(Redact(messageElement.GetString() ?? string.Empty, apiKey));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header?.Delta is { } delta)
        {
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        }

        if (header?.Date is { } date)
        {
            var diff = date - DateTimeOffset.UtcNow;
            return diff < TimeSpan.Zero ? TimeSpan.Zero : diff;
        }

        return null;
    }

    /// <summary>`prompts/17` xaritalash qoidasi: 401/403 → Auth, 429 → RateLimit, 5xx → Server.
    /// Boshqa 4xx (400, 404, 422 va h.k.) → `BadRequest` — PM ko'rsatmasi (2026-09-02):
    /// bu so'rov shaklining o'zi rad etilgani, kalit/limit muammosi emas — bir xil so'rov shu
    /// providerda QAYTA URINISHGA arzimaydi (baribir yana rad etiladi), lekin zanjirdagi
    /// KEYINGI providerga o'tish mantiqli (so'rov shakli providerlar orasida farq qiladi).
    /// Alohida provayderlar (masalan Gemini `API_KEY_INVALID`) buni tana matniga qarab
    /// aniqroq (`Auth`) qilib ustidan yozishi mumkin.</summary>
    public static AiErrorKind ClassifyStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AiErrorKind.Auth,
        (HttpStatusCode)429 => AiErrorKind.RateLimit,
        _ when (int)statusCode >= 500 => AiErrorKind.Server,
        _ when (int)statusCode >= 400 => AiErrorKind.BadRequest,
        _ => AiErrorKind.Unknown,
    };

    /// <summary>
    /// Status kodi + provayder javobining TANASI bo'yicha aniqroq tur. Uchala provayder
    /// noto'g'ri kalitni turlicha bildiradi: Gemini `400 INVALID_ARGUMENT` + `API_KEY_INVALID`,
    /// OpenAI `401 invalid_api_key`, Anthropic `401 authentication_error`. Faqat status kodiga
    /// tayanilsa Gemini'ning noto'g'ri kaliti `BadRequest` ("bu bizning xatomiz") bo'lib
    /// ko'rinardi — admin uchun mutlaqo chalg'ituvchi (P28 jonli tekshiruvda aniqlangan,
    /// 2026-09-02). Shuningdek 404 / `model_not_found` → `ModelNotFound`.
    /// <para>
    /// Tana matni FAQAT tur aniqlash uchun o'qiladi — foydalanuvchiga ko'rsatiladigan xabar
    /// `TestAiProviderCommandHandler`da turdan quriladi, tanadan EMAS.
    /// </para>
    /// </summary>
    public static AiErrorKind ClassifyFailure(HttpStatusCode statusCode, string body)
    {
        var byStatus = ClassifyStatusCode(statusCode);
        if (byStatus is AiErrorKind.RateLimit or AiErrorKind.Server)
        {
            return byStatus;
        }

        if (ContainsAny(body, InvalidKeyMarkers))
        {
            return AiErrorKind.Auth;
        }

        if (statusCode == HttpStatusCode.NotFound || ContainsAny(body, ModelNotFoundMarkers))
        {
            return AiErrorKind.ModelNotFound;
        }

        return byStatus;
    }

    private static bool ContainsAny(string body, IReadOnlyList<string> markers)
    {
        foreach (var marker in markers)
        {
            if (body.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>API kalitini (agar tasodifan tana yoki xabarda uchrasa) `***` bilan almashtiradi —
    /// himoya chuqurligi: provayderlar odatda kalitni js qaytarmaydi, lekin test buni qulflaydi.</summary>
    public static string Redact(string text, string apiKey) =>
        string.IsNullOrEmpty(apiKey) ? text : text.Replace(apiKey, "***", StringComparison.Ordinal);

    private static string BuildErrorMessage(HttpStatusCode statusCode, string body)
    {
        var trimmedBody = body.Length > MaxErrorBodyLength ? string.Concat(body.AsSpan(0, MaxErrorBodyLength), "…") : body;
        return $"AI provayder {(int)statusCode} xato qaytardi: {trimmedBody}";
    }
}

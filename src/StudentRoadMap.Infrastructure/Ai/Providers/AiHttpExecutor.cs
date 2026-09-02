using System.Net;
using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Ai.Providers;

/// <summary>
/// Provayder javobi haqidagi xom natija — `Body` faqat muvaffaqiyatli (`Success = true`)
/// holatda ma'noli, aks holda `ErrorMessage` (kalitsiz, `docs/09` va `prompts/17` MAXSUS
/// DIQQAT #1) ishlatiladi.
/// </summary>
internal sealed record AiHttpOutcome(bool Success, string Body, AiErrorKind ErrorKind, string? ErrorMessage);

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
            return new AiHttpOutcome(false, string.Empty, AiErrorKind.Timeout, "So'rov belgilangan vaqt ichida javob bermadi (timeout).");
        }
        catch (HttpRequestException ex)
        {
            return new AiHttpOutcome(false, string.Empty, AiErrorKind.Unknown, Redact($"Tarmoqqa ulanishda xato: {ex.Message}", apiKey));
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
                return new AiHttpOutcome(false, string.Empty, AiErrorKind.Timeout, "So'rov belgilangan vaqt ichida javob bermadi (timeout).");
            }

            if (response.IsSuccessStatusCode)
            {
                return new AiHttpOutcome(true, body, AiErrorKind.None, null);
            }

            var errorKind = ClassifyStatusCode(response.StatusCode);
            var message = Redact(BuildErrorMessage(response.StatusCode, body), apiKey);
            return new AiHttpOutcome(false, body, errorKind, message);
        }
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

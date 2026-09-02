using System.Text.Json;
using Json.Schema;
using Microsoft.Extensions.Configuration;

namespace StudentRoadMap.Infrastructure.Ai;

/// <summary>Validatsiya natijasi — `docs/09-ai-analiz-moduli.md` 6-bo'lim.</summary>
public enum ValidationOutcome
{
    /// <summary>Barcha 5 bosqichdan o'tdi — natija saqlanadi.</summary>
    Ok,

    /// <summary>Qayta so'ralishi kerak (`AiAnalysis` yangi `AttemptNumber` bilan).</summary>
    Retry,

    /// <summary>
    /// Taqiqlangan atama IKKINCHI urinishda ham chiqdi — natija baribir saqlanadi, lekin
    /// "moderatsiya qilindi" belgisi bilan (`AttentionFlags` ga `MODERATION_REQUIRED`,
    /// `docs/09` 6-bo'lim, 3-band). Ism sizishi (5-bosqich) BU holatga HECH QACHON tushmaydi —
    /// shaxsiy ma'lumot moderatsiya bilan ham DB'da qolib ketmasligi kerak, faqat `Retry`.
    /// </summary>
    Moderated,
}

/// <summary>Qaysi bosqich muvaffaqiyatsiz bo'lganini bildiradi — log/diagnostika uchun.</summary>
public enum AiValidationStage
{
    Parse = 1,
    Schema = 2,
    BannedTerms = 3,
    LanguageLength = 4,
    NameLeak = 5,
}

/// <param name="Outcome">Yakuniy qaror.</param>
/// <param name="FailedStage">`Outcome != Ok` bo'lganda qaysi bosqich to'xtatgani.</param>
/// <param name="Message">Inson o'qiy oladigan sabab (log uchun, sirlarni o'z ichiga olmaydi).</param>
/// <param name="ParsedJson">Muvaffaqiyatli parse qilingan JSON hujjat (1-bosqichdan o'tgan bo'lsa) — keyingi qadamda (masalan `AiAnalysis.ResponseJson`) qayta ishlatish uchun. Chaqiruvchi `Dispose` qilishi kerak.</param>
/// <param name="ModeratedFields">`Moderated` holatida qaysi taqiqlangan atama(lar) topilgani.</param>
public sealed record AiValidationResult(
    ValidationOutcome Outcome,
    AiValidationStage? FailedStage,
    string? Message,
    JsonDocument? ParsedJson,
    IReadOnlyList<string> ModeratedFields);

/// <summary>
/// AI javobini 5 bosqichda tekshiradi — `docs/09-ai-analiz-moduli.md` 6-bo'lim:
/// parse → schema → taqiqlangan atamalar → til/uzunlik → ism sizmasligi.
/// </summary>
public interface IAiResponseValidator
{
    /// <param name="rawJson">Providerdan qaytgan xom JSON matni.</param>
    /// <param name="piiTokens">Shu o'quvchiga tegishli shaxsiy matn bo'laklari (F.I.Sh. so'zlari
    /// va h.k.) — 5-bosqich shularning javobda YO'QLIGINI tekshiradi. Promptga yuborilmagan,
    /// lekin baribir tekshiriladi (`docs/09` 6-bo'lim, 5-band).</param>
    /// <param name="attemptNumber">3-bosqich (taqiqlangan atamalar) uchun: 1-urinishda topilsa
    /// `Retry`, 2+ urinishda topilsa `Moderated` (`docs/09` 6-bo'lim, 3-band).</param>
    AiValidationResult Validate(string? rawJson, IReadOnlyList<string> piiTokens, int attemptNumber = 1);
}

/// <inheritdoc cref="IAiResponseValidator"/>
public sealed class AiResponseValidator : IAiResponseValidator
{
    /// <summary>`docs/09` 6-bo'lim, 3-band — standart ro'yxat, `Ai:BannedTerms` konfiguratsiyasi berilmasa ishlatiladi.</summary>
    private static readonly string[] DefaultBannedTerms =
    [
        "depressiya", "shizofren", "autiz", "ADHD", "SDVG", "buzilish", "kasallik", "patologi",
        "aqli zaif", "dangasa", "qobiliyatsiz", "norma emas", "tashxis", "diagnoz", "davolash", "dori",
    ];

    /// <summary>`docs/09` 6-bo'lim, 4-band: "kiril ulushi > 30% bo'lsa retry".</summary>
    private const double CyrillicRatioThreshold = 0.30;

    /// <summary>Juda qisqa (1–2 harfli) PII bo'laklari (masalan sinf harfi) yolg'on signal berishining oldini oladi.</summary>
    private const int MinPiiTokenLength = 3;

    private readonly IReadOnlyList<string> _bannedTerms;
    private readonly JsonSchema _schema;

    public AiResponseValidator(IConfiguration configuration)
        : this(configuration, AnalysisJsonSchema.Default)
    {
    }

    /// <summary>Testlar uchun — boshqa sxema bilan tekshirish imkoni (masalan eskirgan sxema ssenariysi).</summary>
    internal AiResponseValidator(IConfiguration configuration, JsonSchema schema)
    {
        // `ConfigurationBinder.Get&lt;T&gt;` uchun alohida paket kerak bo'lmasligi uchun
        // (`AppSettingsProvider`dagi bilan bir xil naqsh) — qo'lda `GetChildren()` orqali o'qiladi.
        var configured = configuration.GetSection("Ai:BannedTerms").GetChildren()
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToArray();
        _bannedTerms = configured.Length > 0 ? configured : DefaultBannedTerms;
        _schema = schema;
    }

    public AiValidationResult Validate(string? rawJson, IReadOnlyList<string> piiTokens, int attemptNumber = 1)
    {
        // 1) JSON parse
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return Retry(AiValidationStage.Parse, "AI javobi bo'sh.");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawJson);
        }
        catch (JsonException ex)
        {
            return Retry(AiValidationStage.Parse, $"JSON parse xatosi: {ex.Message}");
        }

        if (document.RootElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            document.Dispose();
            return Retry(AiValidationStage.Parse, "AI javobi 'null' sifatida parse qilindi.");
        }

        // 2) Schema validatsiya (JsonSchema.Net)
        var evaluation = _schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!evaluation.IsValid)
        {
            document.Dispose();
            return Retry(AiValidationStage.Schema, "AI javobi berilgan JSON sxemaga mos emas.");
        }

        // 3) Taqiqlangan atamalar (case-insensitive, o'zak bo'yicha — ro'yxatdagi qiymatlar
        // allaqachon qisqartirilgan o'zaklar, shuning uchun oddiy substring yetarli).
        var bannedHit = _bannedTerms.FirstOrDefault(term => rawJson.Contains(term, StringComparison.OrdinalIgnoreCase));
        if (bannedHit is not null)
        {
            if (attemptNumber <= 1)
            {
                document.Dispose();
                return Retry(AiValidationStage.BannedTerms, $"Taqiqlangan atama topildi: '{bannedHit}'.");
            }

            return new AiValidationResult(
                ValidationOutcome.Moderated,
                AiValidationStage.BannedTerms,
                $"Taqiqlangan atama ikkinchi urinishda ham topildi: '{bannedHit}' — moderatsiya qilindi.",
                document,
                [bannedHit]);
        }

        // 4) Til/uzunlik — matn asosan lotin o'zbek alifbosida ekanini yengil tekshirish.
        var cyrillicRatio = CalculateCyrillicRatio(rawJson);
        if (cyrillicRatio > CyrillicRatioThreshold)
        {
            document.Dispose();
            return Retry(AiValidationStage.LanguageLength, $"Kiril harflari ulushi {cyrillicRatio:P0} — {CyrillicRatioThreshold:P0} chegarasidan yuqori.");
        }

        // 5) Ism sizmasligi — HECH QACHON Moderated emas, faqat Retry (shaxsiy ma'lumot
        // saqlanib qolmasligi shart, taqiqlangan atamalardan farqli o'laroq).
        var leakedToken = piiTokens
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length >= MinPiiTokenLength)
            .FirstOrDefault(t => rawJson.Contains(t.Trim(), StringComparison.OrdinalIgnoreCase));
        if (leakedToken is not null)
        {
            document.Dispose();
            return Retry(AiValidationStage.NameLeak, "AI javobida o'quvchiga tegishli shaxsiy ma'lumot (ism) izi topildi.");
        }

        return new AiValidationResult(ValidationOutcome.Ok, null, null, document, []);
    }

    private static AiValidationResult Retry(AiValidationStage stage, string message) =>
        new(ValidationOutcome.Retry, stage, message, null, []);

    private static double CalculateCyrillicRatio(string text)
    {
        var letterCount = 0;
        var cyrillicCount = 0;

        foreach (var c in text)
        {
            if (!char.IsLetter(c))
            {
                continue;
            }

            letterCount++;
            if (c is >= 'Ѐ' and <= 'ӿ')
            {
                cyrillicCount++;
            }
        }

        return letterCount == 0 ? 0 : (double)cyrillicCount / letterCount;
    }
}

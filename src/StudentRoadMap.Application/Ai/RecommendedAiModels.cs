namespace StudentRoadMap.Application.Ai;

/// <summary>
/// Superadmin "AI sozlamalari" ekranida (P28) TAKLIF sifatida ko'rsatiladigan tavsiya etilgan
/// model ID'lari — majburiy emas, admin `AiProviderConfig.Model`ni istalgan boshqa qiymat
/// bilan almashtirishi mumkin (`docs/09-ai-analiz-moduli.md`: "Model nomlari
/// konfiguratsiyadan olinadi, kodda qattiq yozilmaydi" — bu qoida buzilmaydi, chunki bu
/// ro'yxat DB'ga yozilmaydi, faqat UI'ga taklif beradi). PM ko'rsatmasi (`prompts/17` javob
/// xati, 2026-09-02): "kodda, seed'da emas".
/// </summary>
public static class RecommendedAiModels
{
    /// <summary>Tavsiya: Anthropic. Eslatma: `claude-sonnet-4-5` ESKIRGAN — shu ishlatiladi.</summary>
    public const string Anthropic = "claude-sonnet-5";

    /// <summary>
    /// Tavsiya: Gemini. 2026-09-23: avvalgi `gemini-2.0-flash` Google'da ESKIRGAN — 404 qaytaradi.
    /// `gemini-3.1-flash-lite` mavjudligi jonli loglarda tasdiqlangan (404 emas; o'sha kuni 503
    /// "overloaded" — vaqtinchalik yuklama). Model ro'yxati tez o'zgaradi: UI yordam matni adminni
    /// Google AI Studio → Models ro'yxatiga yo'naltiradi.
    /// </summary>
    public const string Gemini = "gemini-3.1-flash-lite";

    /// <summary>Tavsiya: OpenAI.</summary>
    public const string OpenAi = "gpt-4.1-mini";
}

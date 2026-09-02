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

    /// <summary>Tavsiya: Gemini.</summary>
    public const string Gemini = "gemini-2.0-flash";

    /// <summary>Tavsiya: OpenAI.</summary>
    public const string OpenAi = "gpt-4.1-mini";
}

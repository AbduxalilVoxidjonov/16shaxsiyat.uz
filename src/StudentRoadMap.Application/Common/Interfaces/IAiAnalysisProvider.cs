using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Bitta AI provayder (Gemini/OpenAi/Anthropic yoki `MockAiProvider`) bilan structured-output
/// suhbat abstraksiyasi — `docs/09-ai-analiz-moduli.md` 2-bo'lim. Provider tanlovi hech qachon
/// qattiq yozilmaydi, faqat `IAiProviderResolver` orqali (`prompts/16` qat'iy qoida #1).
/// Bu promptda (P16) implementatsiya YO'Q (`MockAiProvider` bundan mustasno) — haqiqiy
/// providerlar P17'da qo'shiladi.
/// </summary>
public interface IAiAnalysisProvider
{
    /// <summary>Ushbu implementatsiya qaysi providerga mos kelishi.</summary>
    AiProvider Kind { get; }

    /// <summary>
    /// Structured-output (JSON schema bilan cheklangan) javob so'raydi. Tarmoq/auth darajasidagi
    /// xatoni `AiCompletionResult.ErrorKind` orqali qaytaradi — JSON mazmuni tekshiruvi
    /// (`AiResponseValidator`) bu metoddan tashqarida.
    /// </summary>
    Task<AiCompletionResult> CompleteJsonAsync(AiCompletionRequest request, CancellationToken cancellationToken);

    /// <summary>Superadmin "ulanishni tekshirish" amali uchun — kalit va tarmoq holatini tekshiradi.</summary>
    Task<AiHealthResult> CheckHealthAsync(CancellationToken cancellationToken);
}

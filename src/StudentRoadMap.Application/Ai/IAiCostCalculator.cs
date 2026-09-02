using StudentRoadMap.Domain.Ai;

namespace StudentRoadMap.Application.Ai;

/// <summary>
/// `ai_analyses.estimated_cost_usd` uchun taxminiy xarajat hisobi — `docs/09-ai-analiz-moduli.md`
/// 9-bo'lim: `cost = inputTokens/1e6 × inputPer1M + outputTokens/1e6 × outputPer1M`. Narx
/// jadvali konfiguratsiyadan olinadi (`Ai:Pricing:{Provider}:{Model}`), kodda qattiq yozilmaydi
/// (`prompts/17` vazifa #4, cheklov: "Model nomlari kodda qattiq yozilmaydi").
/// </summary>
public interface IAiCostCalculator
{
    /// <summary>
    /// Token soni yoki narx konfiguratsiyasi topilmasa `null` qaytaradi — `0` EMAS
    /// (`docs/07-api-shartnoma.md` "null qoidasi": ma'lumot yo'q bo'lganda hech qachon `0`
    /// ko'rsatilmaydi, chunki bu "arzon xarajat" degan noto'g'ri xulosaga olib kelishi mumkin).
    /// </summary>
    decimal? EstimateCostUsd(AiProvider provider, string model, int? inputTokens, int? outputTokens);
}

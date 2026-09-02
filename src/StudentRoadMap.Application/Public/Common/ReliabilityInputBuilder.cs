using StudentRoadMap.Domain.Scoring;

namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// ⚠️ P12-R1 (MAJBURIY, `prompts/12`): `ReliabilityCalculator.Calculate` sessiya darajasida
/// ishlaydi va uning `Questions` ro'yxatini o'quvchi haqiqatda javob bergan **xronologik**
/// tartibda talab qiladi (`ReliabilityInput.Questions` izohi, `docs/03` §7.1 band 3). Bu
/// ordering mantiqi shu YAGONA joyda yashaydi — `CompleteSessionCommandHandler` faqat shu
/// metodni chaqiradi — shu sabab noto'g'ri ishlatilishi (masalan butun ro'yxatga bittalikda
/// `.OrderBy(q => q.DisplayOrder)`) fizik jihatdan bir joyda oldi olinadi va bitta testda
/// (`ReliabilityInputBuilderTests`) qamrab olinadi.
/// </summary>
internal static class ReliabilityInputBuilder
{
    /// <summary>Sessiyadagi bitta test bloki — sessiya ICHIDAGI tartibi (`AssessmentTest.DisplayOrder`)
    /// va o'sha test ICHIDAGI faol savollari (`Question.DisplayOrder` bilan).</summary>
    public sealed record TestBlock(int DisplayOrderInSession, IReadOnlyList<QuestionMeta> Questions);

    /// <summary>
    /// `testBlocks` — sessiyadagi HAR TO'RTALA (yoki nechta bo'lsa) test bloki, tartibsiz
    /// uzatilishi mumkin (bu yerda `DisplayOrderInSession` bo'yicha saralanadi). Har blok
    /// ICHIDAGI savollar ham bu yerda test ICHIDAGI `DisplayOrder` bo'yicha saralanadi —
    /// chaqiruvchi oldindan saralab chiqarish shart emas.
    /// </summary>
    public static ReliabilityInput Build(
        IReadOnlyList<TestBlock> testBlocks,
        IReadOnlyDictionary<Guid, int> answers,
        IReadOnlyDictionary<Guid, int> durations,
        TimeSpan totalSessionDuration)
    {
        ArgumentNullException.ThrowIfNull(testBlocks);
        ArgumentNullException.ThrowIfNull(answers);
        ArgumentNullException.ThrowIfNull(durations);

        // P12-R1: avval test bloklari SESSIYA ICHIDAGI tartib (`DisplayOrderInSession`) bo'yicha,
        // har bloki ICHIDA esa savollar o'sha TEST ICHIDAGI `DisplayOrder` bo'yicha. Butun
        // ro'yxatga bittalikda `.OrderBy(q => q.DisplayOrder)` qo'llash QAT'IY TAQIQLANADI —
        // `DisplayOrder` har testda `1..N` dan qayta boshlanadi (`1..60`, `1..50`, `1..48`,
        // `1..32`), shunday saralash 4 blokni bir-biriga aralashtirib yuboradi va
        // straight-lining signalini jimgina o'chiradi (P09 QA Bloklovchi-2).
        var orderedQuestions = testBlocks
            .OrderBy(block => block.DisplayOrderInSession)
            .SelectMany(block => block.Questions.OrderBy(q => q.DisplayOrder))
            .ToList();

        return new ReliabilityInput(orderedQuestions, answers, durations, totalSessionDuration);
    }
}

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// `ReliabilityCalculator.Calculate` kirishi — sessiya darajasida, ya'ni **barcha 4 test
/// blokining** savol/javob/davomiylik ma'lumotlarini birlashtirib uzatiladi (`docs/03` §7:
/// "Sessiya darajasida 0–100 ball"). Vaqt (`TotalSessionDuration`) chaqiruvchi tomonidan
/// hisoblanadi va parametr sifatida uzatiladi — tizim soatidan to'g'ridan-to'g'ri foydalanilmaydi.
/// </summary>
/// <param name="Questions">
/// Sessiyadagi barcha savollarning metama'lumoti, **o'quvchi haqiqatda javob bergan xronologik
/// tartibda** (QA Bloklovchi-2 tuzatmasi): masalan `test1.Questions (DisplayOrder bo'yicha) +
/// test2.Questions (DisplayOrder bo'yicha) + test3... + test4...` — testlar ketma-ket
/// to'ldiriladi (`TEST_NOT_UNLOCKED` qoidasi), shuning uchun bu ro'yxatning **tartibi** to'g'ridan
/// -to'g'ri straight-lining hisobida ishlatiladi va QAYTA TARTIBLANMAYDI. `QuestionMeta.DisplayOrder`
/// faqat bitta test ICHIDAGI tartib — sessiya darajasida testlar orasidagi tartibni bilmaydi,
/// shuning uchun `Calculate` uni birlamchi kalit sifatida ishlatmaydi. Chaqiruvchi (Application
/// qatlami) bu ro'yxatni to'g'ri tartibda tuzishga javobgar.
/// </param>
/// <param name="Answers">`QuestionId → xom javob qiymati`.</param>
/// <param name="Durations">`QuestionId → javob berish davomiyligi (ms)`.</param>
/// <param name="TotalSessionDuration">Sessiyaning boshlanishidan yakunlanishigacha bo'lgan vaqt.</param>
public sealed record ReliabilityInput(
    IReadOnlyList<QuestionMeta> Questions,
    IReadOnlyDictionary<Guid, int> Answers,
    IReadOnlyDictionary<Guid, int> Durations,
    TimeSpan TotalSessionDuration);

using StudentRoadMap.Application.Public.SaveAnswers;

namespace StudentRoadMap.Api.Contracts.Public;

/// <summary>
/// `POST /api/public/sessions/tests/{testCode}/answers` so'rov tanasi — `docs/07` 1.6-bo'lim,
/// `docs/18` §4.2. `AssessmentId`/`TestCode` bu yerda YO'Q (token/URL'dan keladi,
/// `StartSessionRequest`dagi naqshning davomi) — mijoz hech qanday ID yubormaydi, faqat
/// `questionId`/`value`/`text`/`selectedValues`/`durationMs`. `Answers` shartnoma bo'yicha
/// MAJBURIY (non-nullable) — Swagger `required` ro'yxatiga tushadi (`AddSwaggerGen`dagi
/// `SupportNonNullableReferenceTypes`, 2026-09-02 tuzatmasi).
/// </summary>
public sealed record SaveAnswersRequest(IReadOnlyList<SaveAnswerItemRequest> Answers)
{
    public SaveAnswersCommand ToCommand(Guid assessmentId, string testCode) =>
        new(
            assessmentId,
            testCode,
            // C# non-nullable annotatsiyasi FAQAT kompilyatsiya vaqtida tekshiriladi —
            // System.Text.Json uni ishga tushirish vaqtida MAJBURLAMAYDI, shu sabab noto'g'ri
            // mijoz JSON'da `answers`ni tushirib qoldirsa baribir `null` kelishi mumkin. Shu
            // sabab mudofaa `?? []` shu yerda qoladi — bo'sh ro'yxat sifatida validatorga
            // uzatiladi (`SaveAnswersCommandValidator` "kamida bitta javob" xatosini beradi,
            // NRE emas).
            (Answers ?? []).Select(a => new SaveAnswerItem(a.QuestionId, a.Value, a.DurationMs, a.Text, a.SelectedValues)).ToList());
}

/// <summary>
/// Bitta javob elementi — `docs/18` §4.2 so'rov shakli. `Value`/`Text`/`SelectedValues`dan
/// AYNAN bittasi to'ldiriladi — savol turiga qarab (`ANSWER_SHAPE_INVALID`).
/// </summary>
public sealed record SaveAnswerItemRequest(
    Guid QuestionId,
    int? Value,
    int DurationMs,
    string? Text = null,
    IReadOnlyList<int>? SelectedValues = null);

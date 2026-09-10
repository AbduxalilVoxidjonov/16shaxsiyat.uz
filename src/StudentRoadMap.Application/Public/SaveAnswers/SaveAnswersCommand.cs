using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.SaveAnswers;

/// <summary>
/// `POST /api/public/sessions/tests/{testCode}/answers` — `docs/07` 1.6-bo'lim, `docs/18` §4.2.
/// `AssessmentId` tokendan (IDOR himoyasi, `CLAUDE.md` 8-qoida) — wire so'rovda
/// (`SaveAnswersRequest`) HECH QANDAY ID (`assessmentTestId` ham) yo'q, faqat
/// `questionId`/`value`/`text`/`selectedValues`/`durationMs`; nishonlangan `AssessmentTest`
/// server tomonida (`AssessmentId` + `TestCode`) orqali topiladi — mijoz boshqa sessiyaning
/// yozuviga hech qanday yo'l bilan ID bera olmaydi.
/// </summary>
public sealed record SaveAnswersCommand(Guid AssessmentId, string TestCode, IReadOnlyList<SaveAnswerItem> Answers)
    : IRequest<Result<SaveAnswersResult>>;

/// <summary>
/// Bitta javob elementi — `docs/18` §4.2 so'rov shakli. `Value`/`Text`/`SelectedValues`dan
/// AYNAN bittasi to'ldirilishi va savol turiga mos bo'lishi shart (`ANSWER_SHAPE_INVALID`).
/// </summary>
public sealed record SaveAnswerItem(
    Guid QuestionId,
    int? Value,
    int DurationMs,
    string? Text = null,
    IReadOnlyList<int>? SelectedValues = null);

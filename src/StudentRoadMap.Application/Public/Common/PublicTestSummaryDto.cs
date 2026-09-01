namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// Sessiya ichidagi bitta test blokining ommaviy ko'rinishi — `docs/07-api-shartnoma.md`
/// 1.2/1.3-bo'limlarida ishlatiladi. `Status` ataylab `string` (domen `TestStatus` enum'i emas):
/// `GET /sessions/me` javobida `"Locked"` qiymati ham kerak (`docs/07` 1.3-bo'lim namunasi),
/// bu esa `AssessmentTest.Status`da mavjud bo'lmagan, faqat proyeksiya darajasidagi holat
/// (`GetSessionStateQueryHandler` hisoblab beradi — oldingi test tugamagan bo'lsa).
///
/// **Diqqat:** `scale`/`scaleDirection` bu yerda YO'Q — `CLAUDE.md` 9-qoida: o'quvchi API'siga
/// hech qachon chiqmaydi.
/// </summary>
public sealed record PublicTestSummaryDto(
    string Code,
    string Status,
    int Answered,
    int Total,
    int Order);

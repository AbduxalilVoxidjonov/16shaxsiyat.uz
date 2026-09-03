using StudentRoadMap.Application.Public.Common;

namespace StudentRoadMap.Application.Public.GetSession;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.3-bo'lim javob shakli.
///
/// `HasPersonalityBattery` — shu sessiyaning dasturida ilmiy shaxsiyat batareyasi (MBTI16/BIG5/
/// RIASEC/ACTIVITY kabi `Standard` + `Scored` metodikalar) bormi. `false` bo'lsa shaxsiyat tipi
/// HECH QACHON hisoblanmaydi, ya'ni natija ekrani o'quvchiga taklif qilinmasligi kerak.
/// Mezon — `Domain.Catalog.PersonalityBattery` (kod satri bo'yicha qidiruv EMAS: frontend
/// ilgari `"MBTI16"` satrini qidirardi va `Custom` dasturda jimgina noto'g'ri ishlardi).
/// </summary>
public sealed record GetSessionStateResult(
    Guid AssessmentId,
    string Status,
    PublicStudentSummaryDto Student,
    DateTimeOffset ExpiresAt,
    string? CurrentTestCode,
    IReadOnlyList<PublicTestSummaryDto> Tests,
    int ProgressPercent,
    bool HasPersonalityBattery);

/// <summary>O'quvchining to'liq ismi hech qachon qaytmaydi (`prompts/10` cheklovi) — faqat qisqartirilgan ism.</summary>
public sealed record PublicStudentSummaryDto(string FirstNameShort, int Grade);

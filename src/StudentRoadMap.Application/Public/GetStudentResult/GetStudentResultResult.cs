namespace StudentRoadMap.Application.Public.GetStudentResult;

/// <summary>
/// `docs/07-api-shartnoma.md` 1.9-bo'lim `200` javob shakli — **qisqartirilgan** natija.
/// `prompts/12` cheklovi: bu turda `MaturityIndex`/`ActivityIndex`/`ReliabilityScore`, xom
/// ballar, bayroqlar yoki `scale`/`scaleDirection` **hech qachon** bo'lmaydi — bu maydonlar
/// shu recordda umuman e'lon qilinmagan, shuning uchun kompilyatsiya darajasida kafolatlangan
/// (`CachedQuestionDto` bilan bir xil naqsh, `Public/Common/CachedCatalogDtos.cs` izohiga qarang).
/// </summary>
public sealed record GetStudentResultResult(
    string PersonalityType,
    string TypeName,
    string ShortDescription,
    IReadOnlyList<string> TopStrengths,
    IReadOnlyList<string> CareerFields,
    string Note);

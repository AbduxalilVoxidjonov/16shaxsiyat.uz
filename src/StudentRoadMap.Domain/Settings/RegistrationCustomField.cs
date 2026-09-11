using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// Superadmin qo'shgan o'z maydoni (masalan "Ota-onangiz kasbi") — `RegistrationFormSettings`
/// (2026-09-11, egasining talabi). <see cref="Type"/> — mavjud `QuestionType` nomlaridan
/// (yangi atama o'ylab topilmadi, `docs/18` §9.6 talabi): FAQAT `ShortText`/`LongText`/`Phone`/
/// `SingleChoice`/`MultiChoice` ruxsat etilgan (`RegistrationFormDefinition.Validate`).
/// </summary>
public sealed record RegistrationCustomField(
    string Code,
    QuestionType Type,
    string LabelUz,
    string? PlaceholderUz,
    RegistrationFieldRequirement Requirement,
    int? MaxLength,
    string? InputPattern,
    IReadOnlyList<RegistrationCustomFieldOption>? Options,
    int Order)
{
    /// <summary>`Code` uchun ruxsat etilgan shakl — anketa savol kodlari bilan bir xil (`QuestionSection.CodePattern`).</summary>
    public static readonly IReadOnlyCollection<QuestionType> AllowedTypes =
    [
        QuestionType.ShortText, QuestionType.LongText, QuestionType.Phone, QuestionType.SingleChoice, QuestionType.MultiChoice,
    ];

    public bool IsChoiceType => Type is QuestionType.SingleChoice or QuestionType.MultiChoice;
}

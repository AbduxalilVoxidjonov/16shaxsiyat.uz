using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Settings;

namespace StudentRoadMap.Application.Admin.Settings.RegistrationForm;

/// <summary>
/// GLOBAL ro'yxatdan o'tish formasi sozlamasining tashqi (API) shakli — `docs/18` §9.6,
/// 2026-09-11/12 (egasining talabi). `GET`/`PUT /api/admin/settings/registration-form` BIR XIL
/// shaklni ishlatadi (to'liq almashtirish semantikasi — `RegistrationFieldsDto` bilan bir xil
/// uslub: enumlar satr sifatida).
/// </summary>
public sealed record RegistrationFormDefinitionDto(
    RegistrationCoreFieldsDto CoreFields,
    IReadOnlyList<RegistrationCustomFieldDto> CustomFields);

public sealed record RegistrationCoreFieldsDto(
    RegistrationCoreFieldDto FullName,
    RegistrationCoreFieldDto BirthDate,
    RegistrationCoreFieldDto Gender,
    RegistrationCoreFieldDto Grade,
    RegistrationCoreFieldDto ClassLetter,
    RegistrationCoreFieldDto Phone,
    RegistrationCoreFieldDto ParentPhone,
    RegistrationCoreFieldDto Email);

public sealed record RegistrationCoreFieldDto(string Requirement, string LabelUz, string? PlaceholderUz, int Order);

public sealed record RegistrationCustomFieldDto(
    string Code,
    string Type,
    string LabelUz,
    string? PlaceholderUz,
    string Requirement,
    int? MaxLength,
    string? InputPattern,
    IReadOnlyList<RegistrationCustomFieldOptionDto>? Options,
    int Order);

public sealed record RegistrationCustomFieldOptionDto(string TextUz, string Value, int Order);

/// <summary>`RegistrationFormDefinitionDto` ↔ `Domain.Settings.RegistrationFormDefinition` xaritalash.</summary>
public static class RegistrationFormSettingsMapping
{
    public static RegistrationFormDefinitionDto ToDto(RegistrationFormDefinition definition) => new(
        ToDto(definition.CoreFields),
        definition.CustomFields.Select(ToDto).ToList());

    private static RegistrationCoreFieldsDto ToDto(RegistrationCoreFields fields) => new(
        ToDto(fields.FullName),
        ToDto(fields.BirthDate),
        ToDto(fields.Gender),
        ToDto(fields.Grade),
        ToDto(fields.ClassLetter),
        ToDto(fields.Phone),
        ToDto(fields.ParentPhone),
        ToDto(fields.Email));

    private static RegistrationCoreFieldDto ToDto(RegistrationCoreField field) =>
        new(field.Requirement.ToString(), field.LabelUz, field.PlaceholderUz, field.Order);

    private static RegistrationCustomFieldDto ToDto(RegistrationCustomField field) => new(
        field.Code,
        field.Type.ToString(),
        field.LabelUz,
        field.PlaceholderUz,
        field.Requirement.ToString(),
        field.MaxLength,
        field.InputPattern,
        field.Options?.Select(o => new RegistrationCustomFieldOptionDto(o.TextUz, o.Value, o.Order)).ToList(),
        field.Order);

    /// <summary>
    /// So'rovdan (`PUT`) domen qiymatiga. Enum satrlarining TO'G'RILIGI validator tomonidan
    /// oldindan tekshirilgan deb hisoblanadi (`UpdateRegistrationFormSettingsCommandValidator`) —
    /// bu yerda `Enum.Parse` xatosi yuzaga kelsa u ushlanmagan istisno sifatida yuqoriga chiqadi
    /// (`RegistrationFieldsMapping.ParseOrDefault` bilan bir xil taxmin).
    /// </summary>
    public static RegistrationFormDefinition ToDomain(RegistrationFormDefinitionDto dto)
    {
        var coreFields = new RegistrationCoreFields(
            ToDomain(dto.CoreFields.FullName),
            ToDomain(dto.CoreFields.BirthDate),
            ToDomain(dto.CoreFields.Gender),
            ToDomain(dto.CoreFields.Grade),
            ToDomain(dto.CoreFields.ClassLetter),
            ToDomain(dto.CoreFields.Phone),
            ToDomain(dto.CoreFields.ParentPhone),
            ToDomain(dto.CoreFields.Email));

        var customFields = dto.CustomFields.Select(ToDomain).ToList();

        return RegistrationFormDefinition.Create(coreFields, customFields);
    }

    private static RegistrationCoreField ToDomain(RegistrationCoreFieldDto dto) =>
        new(Enum.Parse<RegistrationFieldRequirement>(dto.Requirement, ignoreCase: true), dto.LabelUz, dto.PlaceholderUz, dto.Order);

    private static RegistrationCustomField ToDomain(RegistrationCustomFieldDto dto) => new(
        dto.Code,
        Enum.Parse<QuestionType>(dto.Type, ignoreCase: true),
        dto.LabelUz,
        dto.PlaceholderUz,
        Enum.Parse<RegistrationFieldRequirement>(dto.Requirement, ignoreCase: true),
        dto.MaxLength,
        dto.InputPattern,
        dto.Options?.Select(o => new RegistrationCustomFieldOption(o.TextUz, o.Value, o.Order)).ToList(),
        dto.Order);
}

using FluentValidation;
using FluentValidation.Results;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Settings.RegistrationForm;

/// <summary>
/// Oddiy shakl xatolarini (bo'sh maydon, uzunlik, noma'lum enum nomi) `400 VALIDATION_ERROR`
/// bilan ushlaydi — murakkab (o'zaro bog'liq) qoidalar `RegistrationFormDefinition.Validate`da
/// (Domain, o'z kodlari bilan, `docs/06` §6). Bitta `Custom` qoida ishlatilgan (nested
/// `RuleFor(x => x.Definition.CoreFields...)` o'rniga) — `Definition`/`CoreFields` `null` bo'lishi
/// mumkin bo'lgan holatda ifoda daraxtidagi `null` havolaga kirish (`NullReferenceException`,
/// FluentValidation'ning qiymat olish bosqichi `When` shartidan OLDIN ishlaydi) `500`ga olib
/// kelmasligi uchun.
/// </summary>
public sealed class UpdateRegistrationFormSettingsCommandValidator : AbstractValidator<UpdateRegistrationFormSettingsCommand>
{
    private static readonly string[] AllowedRequirements = Enum.GetNames<RegistrationFieldRequirement>();

    private static readonly string[] AllowedCustomFieldTypes =
    [
        nameof(QuestionType.ShortText), nameof(QuestionType.LongText), nameof(QuestionType.Phone),
        nameof(QuestionType.SingleChoice), nameof(QuestionType.MultiChoice),
    ];

    public UpdateRegistrationFormSettingsCommandValidator()
    {
        RuleFor(x => x.Definition).NotNull();

        RuleFor(x => x).Custom(Validate);
    }

    private static void Validate(UpdateRegistrationFormSettingsCommand command, ValidationContext<UpdateRegistrationFormSettingsCommand> context)
    {
        var definition = command.Definition;
        if (definition is null)
        {
            return;
        }

        var coreFields = definition.CoreFields;
        if (coreFields is null)
        {
            context.AddFailure(new ValidationFailure("Definition.CoreFields", "Asosiy maydonlar (coreFields) kiritilishi shart."));
        }
        else
        {
            ValidateCoreField(context, "Definition.CoreFields.FullName", coreFields.FullName);
            ValidateCoreField(context, "Definition.CoreFields.BirthDate", coreFields.BirthDate);
            ValidateCoreField(context, "Definition.CoreFields.Gender", coreFields.Gender);
            ValidateCoreField(context, "Definition.CoreFields.Grade", coreFields.Grade);
            ValidateCoreField(context, "Definition.CoreFields.ClassLetter", coreFields.ClassLetter);
            ValidateCoreField(context, "Definition.CoreFields.Phone", coreFields.Phone);
            ValidateCoreField(context, "Definition.CoreFields.ParentPhone", coreFields.ParentPhone);
            ValidateCoreField(context, "Definition.CoreFields.Email", coreFields.Email);
        }

        var customFields = definition.CustomFields;
        if (customFields is null)
        {
            context.AddFailure(new ValidationFailure("Definition.CustomFields", "O'z maydonlar ro'yxati (customFields) kiritilishi shart — bo'sh ro'yxat ([]) ruxsat etiladi."));
            return;
        }

        for (var i = 0; i < customFields.Count; i++)
        {
            ValidateCustomField(context, $"Definition.CustomFields[{i}]", customFields[i]);
        }
    }

    private static void ValidateCoreField(ValidationContext<UpdateRegistrationFormSettingsCommand> context, string path, RegistrationCoreFieldDto? field)
    {
        if (field is null)
        {
            context.AddFailure(new ValidationFailure(path, "Maydon sozlamasi kiritilishi shart."));
            return;
        }

        if (string.IsNullOrWhiteSpace(field.LabelUz))
        {
            context.AddFailure(new ValidationFailure($"{path}.LabelUz", "Maydon nomi (yorlig'i) kiritilishi shart."));
        }
        else if (field.LabelUz.Length > 200)
        {
            context.AddFailure(new ValidationFailure($"{path}.LabelUz", "Maydon nomi 200 belgidan oshmasligi kerak."));
        }

        if (field.PlaceholderUz is { Length: > 200 })
        {
            context.AddFailure(new ValidationFailure($"{path}.PlaceholderUz", "Placeholder 200 belgidan oshmasligi kerak."));
        }

        if (!AllowedRequirements.Contains(field.Requirement, StringComparer.OrdinalIgnoreCase))
        {
            context.AddFailure(new ValidationFailure($"{path}.Requirement", $"Holat quyidagilardan biri bo'lishi kerak: {string.Join(", ", AllowedRequirements)}."));
        }
    }

    private static void ValidateCustomField(ValidationContext<UpdateRegistrationFormSettingsCommand> context, string path, RegistrationCustomFieldDto? field)
    {
        if (field is null)
        {
            context.AddFailure(new ValidationFailure(path, "Maydon sozlamasi kiritilishi shart."));
            return;
        }

        if (string.IsNullOrWhiteSpace(field.Code))
        {
            context.AddFailure(new ValidationFailure($"{path}.Code", "Maydon kodi kiritilishi shart."));
        }
        else if (field.Code.Length > 20)
        {
            context.AddFailure(new ValidationFailure($"{path}.Code", "Maydon kodi 20 belgidan oshmasligi kerak."));
        }

        if (!AllowedCustomFieldTypes.Contains(field.Type, StringComparer.OrdinalIgnoreCase))
        {
            context.AddFailure(new ValidationFailure($"{path}.Type", $"Maydon turi quyidagilardan biri bo'lishi kerak: {string.Join(", ", AllowedCustomFieldTypes)}."));
        }

        if (string.IsNullOrWhiteSpace(field.LabelUz))
        {
            context.AddFailure(new ValidationFailure($"{path}.LabelUz", "Maydon nomi (yorlig'i) kiritilishi shart."));
        }
        else if (field.LabelUz.Length > 200)
        {
            context.AddFailure(new ValidationFailure($"{path}.LabelUz", "Maydon nomi 200 belgidan oshmasligi kerak."));
        }

        if (field.PlaceholderUz is { Length: > 200 })
        {
            context.AddFailure(new ValidationFailure($"{path}.PlaceholderUz", "Placeholder 200 belgidan oshmasligi kerak."));
        }

        if (!AllowedRequirements.Contains(field.Requirement, StringComparer.OrdinalIgnoreCase))
        {
            context.AddFailure(new ValidationFailure($"{path}.Requirement", $"Holat quyidagilardan biri bo'lishi kerak: {string.Join(", ", AllowedRequirements)}."));
        }

        if (field.MaxLength is < 1 or > 4000)
        {
            context.AddFailure(new ValidationFailure($"{path}.MaxLength", "MaxLength 1 dan 4000 gacha bo'lishi kerak."));
        }

        if (field.InputPattern is { Length: > 200 })
        {
            context.AddFailure(new ValidationFailure($"{path}.InputPattern", "InputPattern 200 belgidan oshmasligi kerak."));
        }

        if (field.Options is null)
        {
            return;
        }

        for (var i = 0; i < field.Options.Count; i++)
        {
            var option = field.Options[i];
            if (string.IsNullOrWhiteSpace(option.TextUz) || option.TextUz.Length > 200)
            {
                context.AddFailure(new ValidationFailure($"{path}.Options[{i}].TextUz", "Variant matni kiritilishi va 200 belgidan oshmasligi kerak."));
            }

            if (string.IsNullOrWhiteSpace(option.Value) || option.Value.Length > 100)
            {
                context.AddFailure(new ValidationFailure($"{path}.Options[{i}].Value", "Variant qiymati kiritilishi va 100 belgidan oshmasligi kerak."));
            }
        }
    }
}

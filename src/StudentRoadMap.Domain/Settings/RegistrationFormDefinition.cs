using System.Text.RegularExpressions;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Settings;

/// <summary>
/// Ro'yxatdan o'tish formasining TO'LIQ shakli — `RegistrationFormSettings` agregatining
/// qiymati (2026-09-11, egasining talabi: "Sozlamalar" sahifasidan boshqariladigan GLOBAL
/// sozlama, `AssessmentProgram.RegistrationFields`ning o'rnini bosadi — `docs/18` §9.6).
///
/// <para>
/// <b>Nima uchun endi global?</b> Ikki joyda bir xil sozlama (har dasturda alohida VA global)
/// ushbu loyihada qayta-qayta chalkashlikka olib kelgan naqsh edi. Egasi global variantni
/// tanladi — <see cref="RegistrationFormSettings"/> yagona manba.
/// </para>
/// </summary>
public sealed partial record RegistrationFormDefinition(
    RegistrationCoreFields CoreFields,
    IReadOnlyList<RegistrationCustomField> CustomFields)
{
    /// <summary>
    /// `docs/18` §9.6 jadvali — 2026-09-11 gacha bo'lgan `RegistrationFields.Default` bilan
    /// BAYT-BAYT mos (`gender` ham `Required`, o'sha kuni qilingan tuzatish bilan bir xil).
    /// Sozlama umuman yaratilmagan bo'lsa ("NULL = standart" naqshi) shu qiymat ishlatiladi.
    /// </summary>
    public static readonly RegistrationFormDefinition Default = new(
        CoreFields: new RegistrationCoreFields(
            FullName: new RegistrationCoreField(RegistrationFieldRequirement.Required, "F.I.Sh.", null, 1),
            BirthDate: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Tug'ilgan sana", null, 2),
            Gender: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Jins", null, 3),
            Grade: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Sinf", null, 4),
            ClassLetter: new RegistrationCoreField(RegistrationFieldRequirement.Optional, "Sinf harfi", null, 5),
            Phone: new RegistrationCoreField(RegistrationFieldRequirement.Required, "Telefon raqami", null, 6),
            ParentPhone: new RegistrationCoreField(RegistrationFieldRequirement.Optional, "Ota-ona telefoni", null, 7),
            Email: new RegistrationCoreField(RegistrationFieldRequirement.Optional, "Email", null, 8)),
        CustomFields: []);

    /// <summary>Barcha invariantlarni tekshirib yangi qiymat yaratadi — buzilsa `DomainException` (`docs/06` §6).</summary>
    public static RegistrationFormDefinition Create(RegistrationCoreFields coreFields, IReadOnlyList<RegistrationCustomField> customFields)
    {
        ArgumentNullException.ThrowIfNull(coreFields);
        ArgumentNullException.ThrowIfNull(customFields);

        Validate(coreFields, customFields);

        return new RegistrationFormDefinition(coreFields, customFields);
    }

    private static void Validate(RegistrationCoreFields coreFields, IReadOnlyList<RegistrationCustomField> customFields)
    {
        // `fullName` HAR DOIM majburiy — RegistrationCoreField.cs izohiga qarang.
        if (coreFields.FullName.Requirement != RegistrationFieldRequirement.Required)
        {
            throw new DomainException(
                "REGISTRATION_FORM_FULL_NAME_LOCKED",
                "F.I.Sh. maydoni har doim majburiy bo'lishi kerak — uni o'zgartirib bo'lmaydi.");
        }

        var seenCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in customFields)
        {
            ValidateCode(field.Code);

            if (!seenCodes.Add(field.Code))
            {
                throw new DomainException(
                    "REGISTRATION_FORM_FIELD_CODE_DUPLICATE",
                    $"'{field.Code}' kodi bilan bir nechta maydon bor.");
            }

            if (RegistrationCoreFields.ReservedCodes.Contains(field.Code))
            {
                throw new DomainException(
                    "REGISTRATION_FORM_FIELD_CODE_DUPLICATE",
                    $"'{field.Code}' — asosiy maydon nomi, o'z maydoniga kod sifatida ishlatib bo'lmaydi.");
            }

            if (!RegistrationCustomField.AllowedTypes.Contains(field.Type))
            {
                throw new ArgumentException(
                    $"'{field.Code}' maydonining turi ('{field.Type}') ro'yxatdan o'tish formasida ruxsat etilmagan.",
                    nameof(customFields));
            }

            if (field.IsChoiceType)
            {
                var options = field.Options ?? [];
                if (options.Count < 2)
                {
                    throw new DomainException(
                        "REGISTRATION_FORM_CHOICE_OPTIONS_INSUFFICIENT",
                        $"'{field.Code}' maydonida kamida 2 ta tanlov varianti bo'lishi kerak.");
                }

                var seenValues = new HashSet<string>(StringComparer.Ordinal);
                foreach (var option in options)
                {
                    if (!seenValues.Add(option.Value))
                    {
                        throw new DomainException(
                            "REGISTRATION_FORM_OPTION_VALUE_DUPLICATE",
                            $"'{field.Code}' maydonida '{option.Value}' qiymati takrorlangan.");
                    }
                }
            }
        }
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern().IsMatch(code))
        {
            throw new DomainException(
                "REGISTRATION_FORM_FIELD_CODE_INVALID",
                $"'{code}' kodi noto'g'ri — faqat lotin harf, raqam, '-' va '_' belgilaridan (1-20 ta) iborat bo'lishi mumkin.");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,20}$")]
    private static partial Regex CodePattern();
}

using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Common.Models;

/// <summary>
/// `AssessmentProgram.RegistrationFields` ning tashqi (API) shakli — P52 kengaytmasi
/// (`docs/18` §9.5). Har bir maydon `"Hidden"`/`"Optional"`/`"Required"` satri. Ommaviy
/// (`GET /api/public/schools/{slug}`) va admin (`GET/POST/PUT /api/admin/programs`) API'lari
/// BIR XIL shaklni ishlatadi.
/// </summary>
public sealed record RegistrationFieldsDto(
    string BirthDate,
    string Gender,
    string Grade,
    string ClassLetter,
    string Phone,
    string ParentPhone,
    string Email);

/// <summary>
/// `RegistrationFieldsDto` ↔ `Domain.Catalog.RegistrationFields` xaritalash — admin va ommaviy
/// mapping'lar orasida takrorlanmasin (`SchoolMapping`/`ProgramMapping` naqshiga o'xshash).
/// </summary>
public static class RegistrationFieldsMapping
{
    public static RegistrationFieldsDto ToDto(RegistrationFields fields) => new(
        fields.BirthDate.ToString(),
        fields.Gender.ToString(),
        fields.Grade.ToString(),
        fields.ClassLetter.ToString(),
        fields.Phone.ToString(),
        fields.ParentPhone.ToString(),
        fields.Email.ToString());

    /// <summary>
    /// So'rovdan (admin `Create`/`Update`) domen qiymat obyektiga. `input is null` — mijoz
    /// bu maydonni umuman yubormadi, `null` qaytadi (dasturda "standart qiymatlar" saqlanadi,
    /// `AssessmentProgram.ResolveRegistrationFields`). `input` bor bo'lsa, har bir ICHKI maydon
    /// ham mustaqil ixtiyoriy — bo'sh qoldirilgani standart (`RegistrationFields.Default`)dagi
    /// mos qiymat bilan to'ldiriladi.
    /// </summary>
    public static RegistrationFields? ToDomain(RegistrationFieldsInput? input)
    {
        if (input is null)
        {
            return null;
        }

        return new RegistrationFields(
            ParseOrDefault(input.BirthDate, RegistrationFields.Default.BirthDate),
            ParseOrDefault(input.Gender, RegistrationFields.Default.Gender),
            ParseOrDefault(input.Grade, RegistrationFields.Default.Grade),
            ParseOrDefault(input.ClassLetter, RegistrationFields.Default.ClassLetter),
            ParseOrDefault(input.Phone, RegistrationFields.Default.Phone),
            ParseOrDefault(input.ParentPhone, RegistrationFields.Default.ParentPhone),
            ParseOrDefault(input.Email, RegistrationFields.Default.Email));
    }

    private static RegistrationFieldRequirement ParseOrDefault(string? value, RegistrationFieldRequirement fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : Enum.Parse<RegistrationFieldRequirement>(value, ignoreCase: true);
}

/// <summary>
/// `POST`/`PUT /api/admin/programs` so'rov tanasidagi ixtiyoriy `registrationFields` obyekti —
/// har bir maydon mustaqil ixtiyoriy (`null` — standart qiymat, `RegistrationFieldsMapping.ToDomain`).
/// </summary>
public sealed record RegistrationFieldsInput(
    string? BirthDate = null,
    string? Gender = null,
    string? Grade = null,
    string? ClassLetter = null,
    string? Phone = null,
    string? ParentPhone = null,
    string? Email = null);

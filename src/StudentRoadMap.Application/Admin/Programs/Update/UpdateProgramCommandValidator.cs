using FluentValidation;
using StudentRoadMap.Application.Common.Models;

namespace StudentRoadMap.Application.Admin.Programs.Update;

/// <summary>`public` — `CreateSchoolCommandValidator` izohidagi sabab bilan bir xil (`AssemblyScanner`).</summary>
public sealed class UpdateProgramCommandValidator : AbstractValidator<UpdateProgramCommand>
{
    public UpdateProgramCommandValidator()
    {
        RuleFor(x => x.NameUz).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Visibility)
            .Must(v => Enum.TryParse<Domain.Catalog.ProgramVisibility>(v, ignoreCase: true, out _))
            .WithMessage("Ko'rinish 'Public' yoki 'Assigned' bo'lishi kerak.");

        RuleFor(x => x.RegistrationMode)
            .Must(v => Enum.TryParse<Domain.Catalog.RegistrationMode>(v, ignoreCase: true, out _))
            .WithMessage("Ro'yxatdan o'tish rejimi 'Full' yoki 'None' bo'lishi kerak.");

        // P52 kengaytmasi (2026-09-11, `docs/18` §9.5) — `CreateProgramCommandValidator` bilan bir xil qoida.
        RuleFor(x => x.RegistrationFields)
            .Must(BeValidFieldsOrNull)
            .WithMessage("Ro'yxatdan o'tish maydonlari 'Hidden', 'Optional' yoki 'Required' bo'lishi kerak.");
    }

    private static bool BeValidFieldsOrNull(RegistrationFieldsInput? input)
    {
        if (input is null)
        {
            return true;
        }

        return IsValidOrEmpty(input.BirthDate)
            && IsValidOrEmpty(input.Gender)
            && IsValidOrEmpty(input.Grade)
            && IsValidOrEmpty(input.ClassLetter)
            && IsValidOrEmpty(input.Phone)
            && IsValidOrEmpty(input.ParentPhone)
            && IsValidOrEmpty(input.Email);
    }

    private static bool IsValidOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || Enum.TryParse<Domain.Catalog.RegistrationFieldRequirement>(value, ignoreCase: true, out _);
}

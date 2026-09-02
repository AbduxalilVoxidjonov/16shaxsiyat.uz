using FluentValidation;

namespace StudentRoadMap.Application.Admin.Programs.Create;

/// <summary>`public` — `CreateSchoolCommandValidator` izohidagi sabab bilan bir xil (`AssemblyScanner`).</summary>
public sealed class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Dastur kodi kiritilishi shart.")
            .MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Dastur kodi faqat katta lotin harflari, raqam, '-'/'_' belgilaridan iborat bo'lishi kerak.");

        RuleFor(x => x.NameUz)
            .NotEmpty().WithMessage("Dastur nomi kiritilishi shart.")
            .MaximumLength(150);

        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Visibility)
            .Must(v => Enum.TryParse<Domain.Catalog.ProgramVisibility>(v, ignoreCase: true, out _))
            .WithMessage("Ko'rinish 'Public' yoki 'Assigned' bo'lishi kerak.");
    }
}

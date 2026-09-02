using FluentValidation;

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
    }
}

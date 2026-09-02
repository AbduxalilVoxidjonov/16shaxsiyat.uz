using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Create;

public sealed class CreateTestScaleCommandValidator : AbstractValidator<CreateTestScaleCommand>
{
    public CreateTestScaleCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Shkala kodi kiritilishi shart.")
            .MaximumLength(10)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Shkala kodi faqat katta lotin harflari, raqam, '-'/'_' belgilaridan iborat bo'lishi kerak.");

        RuleFor(x => x.NameUz).NotEmpty().WithMessage("Shkala nomi kiritilishi shart.").MaximumLength(120);

        RuleForEach(x => x.InterpretationBands).ChildRules(band =>
        {
            band.RuleFor(b => b.From).GreaterThanOrEqualTo(0);
            band.RuleFor(b => b.To).LessThanOrEqualTo(100);
            band.RuleFor(b => b.Label).NotEmpty().MaximumLength(40);
        });
    }
}

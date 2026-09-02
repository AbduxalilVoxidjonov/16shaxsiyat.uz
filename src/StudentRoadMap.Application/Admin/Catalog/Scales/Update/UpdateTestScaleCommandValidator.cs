using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Scales.Update;

public sealed class UpdateTestScaleCommandValidator : AbstractValidator<UpdateTestScaleCommand>
{
    public UpdateTestScaleCommandValidator()
    {
        RuleFor(x => x.NameUz).NotEmpty().WithMessage("Shkala nomi kiritilishi shart.").MaximumLength(120);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.InterpretationBands).ChildRules(band =>
        {
            band.RuleFor(b => b.From).GreaterThanOrEqualTo(0);
            band.RuleFor(b => b.To).LessThanOrEqualTo(100);
            band.RuleFor(b => b.Label).NotEmpty().MaximumLength(40);
        });
    }
}

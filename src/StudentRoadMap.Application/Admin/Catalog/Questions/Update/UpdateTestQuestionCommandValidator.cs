using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Update;

public sealed class UpdateTestQuestionCommandValidator : AbstractValidator<UpdateTestQuestionCommand>
{
    public UpdateTestQuestionCommandValidator()
    {
        RuleFor(x => x.TextUz).NotEmpty().WithMessage("Savol matni kiritilishi shart.");
        RuleFor(x => x.Direction).Must(d => d is 1 or -1).When(x => x.Direction.HasValue).WithMessage("Yo'nalish faqat +1 yoki -1 bo'lishi mumkin.");
        RuleFor(x => x.Weight).GreaterThan(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.Order).GreaterThan(0).When(x => x.Order.HasValue);

        RuleFor(x => x.SectionCode).MaximumLength(20).When(x => x.SectionCode is not null);
        RuleFor(x => x.Placeholder).MaximumLength(200).When(x => x.Placeholder is not null);
        RuleFor(x => x.InputPattern).MaximumLength(200).When(x => x.InputPattern is not null);
        RuleFor(x => x.MaxLength).InclusiveBetween(1, 4000).When(x => x.MaxLength.HasValue);
        RuleFor(x => x.MinSelections).GreaterThanOrEqualTo(0).When(x => x.MinSelections.HasValue);
        RuleFor(x => x.MaxSelections).GreaterThanOrEqualTo(1).When(x => x.MaxSelections.HasValue);

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.TextUz).NotEmpty().WithMessage("Variant matni kiritilishi shart.");
        });
    }
}

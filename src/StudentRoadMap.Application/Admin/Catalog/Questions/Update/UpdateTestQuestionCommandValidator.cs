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
    }
}

using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Reorder;

public sealed class ReorderTestQuestionsCommandValidator : AbstractValidator<ReorderTestQuestionsCommand>
{
    public ReorderTestQuestionsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Kamida bitta savol berilishi kerak.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DisplayOrder).GreaterThan(0);
        });
    }
}

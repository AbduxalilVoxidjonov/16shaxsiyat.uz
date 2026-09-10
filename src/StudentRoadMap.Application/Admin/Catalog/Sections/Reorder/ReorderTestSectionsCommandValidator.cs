using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Reorder;

public sealed class ReorderTestSectionsCommandValidator : AbstractValidator<ReorderTestSectionsCommand>
{
    public ReorderTestSectionsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Kamida bitta bo'lim berilishi kerak.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }
}

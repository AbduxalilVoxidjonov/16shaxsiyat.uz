using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Update;

public sealed class UpdateTestSectionCommandValidator : AbstractValidator<UpdateTestSectionCommand>
{
    public UpdateTestSectionCommandValidator()
    {
        RuleFor(x => x.TitleUz).NotEmpty().WithMessage("Bo'lim sarlavhasi kiritilishi shart.").MaximumLength(200);
        RuleFor(x => x.DescriptionUz).MaximumLength(1000).When(x => x.DescriptionUz is not null);
    }
}

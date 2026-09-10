using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Sections.Create;

public sealed class CreateTestSectionCommandValidator : AbstractValidator<CreateTestSectionCommand>
{
    public CreateTestSectionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Bo'lim kodi kiritilishi shart.")
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Bo'lim kodi faqat lotin harflari, raqam, '-'/'_' belgilaridan iborat bo'lishi kerak.");

        RuleFor(x => x.TitleUz).NotEmpty().WithMessage("Bo'lim sarlavhasi kiritilishi shart.").MaximumLength(200);
        RuleFor(x => x.DescriptionUz).MaximumLength(1000).When(x => x.DescriptionUz is not null);
    }
}

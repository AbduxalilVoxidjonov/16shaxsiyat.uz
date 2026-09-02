using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Duplicate;

public sealed class DuplicateCatalogTestCommandValidator : AbstractValidator<DuplicateCatalogTestCommand>
{
    public DuplicateCatalogTestCommandValidator()
    {
        RuleFor(x => x.NewCode)
            .NotEmpty().WithMessage("Yangi anketa kodi kiritilishi shart.")
            .MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Anketa kodi faqat katta lotin harflari, raqam, '-'/'_' belgilaridan iborat bo'lishi kerak.");
    }
}

using FluentValidation;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Update;

public sealed class UpdateCatalogTestCommandValidator : AbstractValidator<UpdateCatalogTestCommand>
{
    public UpdateCatalogTestCommandValidator()
    {
        RuleFor(x => x.NameUz).NotEmpty().WithMessage("Anketa nomi kiritilishi shart.").MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0);
    }
}

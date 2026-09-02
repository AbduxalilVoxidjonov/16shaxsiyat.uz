using FluentValidation;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Create;

/// <summary>`public` — `AssemblyScanner` faqat public validatorlarni ko'radi (`docs/06` §8 2026-08-31 qarori).</summary>
public sealed class CreateCatalogTestCommandValidator : AbstractValidator<CreateCatalogTestCommand>
{
    public CreateCatalogTestCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Anketa kodi kiritilishi shart.")
            .MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Anketa kodi faqat katta lotin harflari, raqam, '-'/'_' belgilaridan iborat bo'lishi kerak.");

        RuleFor(x => x.NameUz)
            .NotEmpty().WithMessage("Anketa nomi kiritilishi shart.")
            .MaximumLength(150);

        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);

        RuleFor(x => x.PageSize).GreaterThan(0).When(x => x.PageSize.HasValue);

        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);

        RuleFor(x => x.ScoringMode)
            .Must(v => Enum.TryParse<TestScoringMode>(v, ignoreCase: true, out _))
            .WithMessage("Ball hisoblash rejimi 'Scored' yoki 'Survey' bo'lishi kerak.")
            .When(x => !string.IsNullOrWhiteSpace(x.ScoringMode));
    }
}

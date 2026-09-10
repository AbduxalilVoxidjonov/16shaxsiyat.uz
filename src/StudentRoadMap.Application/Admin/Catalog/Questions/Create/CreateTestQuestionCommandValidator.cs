using FluentValidation;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Create;

public sealed class CreateTestQuestionCommandValidator : AbstractValidator<CreateTestQuestionCommand>
{
    public CreateTestQuestionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Savol kodi kiritilishi shart.")
            .MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Savol kodi faqat lotin harflari, raqam, '-'/'_' belgilaridan iborat bo'lishi kerak.");

        RuleFor(x => x.Order).GreaterThan(0);
        RuleFor(x => x.TextUz).NotEmpty().WithMessage("Savol matni kiritilishi shart.");

        RuleFor(x => x.Type)
            .Must(v => Enum.TryParse<QuestionType>(v, ignoreCase: true, out _))
            .WithMessage("Savol turi noto'g'ri.");

        RuleFor(x => x.Scale).NotEmpty().WithMessage("Shkala kiritilishi shart.").MaximumLength(10);
        RuleFor(x => x.Direction).Must(d => d is 1 or -1).WithMessage("Yo'nalish faqat +1 yoki -1 bo'lishi mumkin.");
        RuleFor(x => x.Weight).GreaterThan(0);

        // `docs/18` §2.2/§2.3 — sonli maydonlar uchun erta (400) tekshiruv; regex kompilyatsiyasi
        // esa handler'da (`INPUT_PATTERN_INVALID`, `CachedInputPatternMatcher` bilan bir xil qoida).
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

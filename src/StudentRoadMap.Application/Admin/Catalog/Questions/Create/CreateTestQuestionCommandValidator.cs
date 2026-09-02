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
    }
}

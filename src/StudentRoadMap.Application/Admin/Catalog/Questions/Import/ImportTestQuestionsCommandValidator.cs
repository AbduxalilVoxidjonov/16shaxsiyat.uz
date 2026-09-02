using FluentValidation;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Import;

public sealed class ImportTestQuestionsCommandValidator : AbstractValidator<ImportTestQuestionsCommand>
{
    public ImportTestQuestionsCommandValidator()
    {
        RuleFor(x => x.Questions).NotEmpty().WithMessage("Kamida bitta savol bo'lishi kerak.");

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Code).NotEmpty().MaximumLength(20);
            question.RuleFor(q => q.Order).GreaterThan(0);
            question.RuleFor(q => q.TextUz).NotEmpty();
            question.RuleFor(q => q.Type).Must(v => Enum.TryParse<QuestionType>(v, ignoreCase: true, out _)).WithMessage("Savol turi noto'g'ri.");
            question.RuleFor(q => q.Scale).NotEmpty().MaximumLength(10);
            question.RuleFor(q => q.Direction).Must(d => d is 1 or -1).WithMessage("Yo'nalish faqat +1 yoki -1 bo'lishi mumkin.");
            question.RuleFor(q => q.Weight).GreaterThan(0);
        });
    }
}

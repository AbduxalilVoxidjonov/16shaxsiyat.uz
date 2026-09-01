using FluentValidation;

namespace StudentRoadMap.Application.Public.GetTestQuestions;

/// <summary>`public` — `AssemblyScanner` faqat ochiq validatorlarni topadi (P10 tuzatmasi).</summary>
public sealed class GetTestQuestionsQueryValidator : AbstractValidator<GetTestQuestionsQuery>
{
    public GetTestQuestionsQueryValidator()
    {
        RuleFor(x => x.TestCode)
            .NotEmpty().WithMessage("Test kodi ko'rsatilishi shart.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Sahifa raqami 1 dan boshlanadi.");
    }
}

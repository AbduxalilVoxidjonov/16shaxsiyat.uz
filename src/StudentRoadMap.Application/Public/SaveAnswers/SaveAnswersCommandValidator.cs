using FluentValidation;

namespace StudentRoadMap.Application.Public.SaveAnswers;

/// <summary>`public` — `AssemblyScanner` faqat ochiq validatorlarni topadi (P10 tuzatmasi).</summary>
public sealed class SaveAnswersCommandValidator : AbstractValidator<SaveAnswersCommand>
{
    /// <summary>`prompts/11`: "bir so'rovda maksimum 50 javob; oshsa 400".</summary>
    public const int MaxAnswersPerRequest = 50;

    public SaveAnswersCommandValidator()
    {
        RuleFor(x => x.TestCode)
            .NotEmpty().WithMessage("Test kodi ko'rsatilishi shart.");

        RuleFor(x => x.Answers)
            .NotEmpty().WithMessage("Kamida bitta javob yuborilishi kerak.")
            .Must(answers => answers.Count <= MaxAnswersPerRequest)
            .WithMessage($"Bir so'rovda maksimum {MaxAnswersPerRequest} ta javob yuborish mumkin.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId)
                .NotEmpty().WithMessage("Savol ID'si ko'rsatilishi shart.");

            answer.RuleFor(a => a.DurationMs)
                .GreaterThanOrEqualTo(0).WithMessage("Javob berish davomiyligi manfiy bo'lishi mumkin emas.");
        });
    }
}

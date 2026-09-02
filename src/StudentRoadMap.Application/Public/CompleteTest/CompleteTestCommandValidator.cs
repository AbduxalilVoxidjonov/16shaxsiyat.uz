using FluentValidation;

namespace StudentRoadMap.Application.Public.CompleteTest;

/// <summary>`public` — `AssemblyScanner` faqat ochiq validatorlarni topadi (P10 tuzatmasi).</summary>
public sealed class CompleteTestCommandValidator : AbstractValidator<CompleteTestCommand>
{
    public CompleteTestCommandValidator()
    {
        RuleFor(x => x.TestCode)
            .NotEmpty().WithMessage("Test kodi ko'rsatilishi shart.");
    }
}

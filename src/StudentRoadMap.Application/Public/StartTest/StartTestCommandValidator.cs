using FluentValidation;

namespace StudentRoadMap.Application.Public.StartTest;

/// <summary>
/// `public` (`internal` emas) — `AssemblyScanner.FindValidatorsInAssembly` faqat ochiq
/// validatorlarni topadi (`StartSessionCommandValidator` izohidagi P10 tuzatmasi bilan bir xil).
/// </summary>
public sealed class StartTestCommandValidator : AbstractValidator<StartTestCommand>
{
    public StartTestCommandValidator()
    {
        RuleFor(x => x.TestCode)
            .NotEmpty().WithMessage("Test kodi ko'rsatilishi shart.");
    }
}

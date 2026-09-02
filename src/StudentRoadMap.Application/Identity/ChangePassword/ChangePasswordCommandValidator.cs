using FluentValidation;

namespace StudentRoadMap.Application.Identity.ChangePassword;

/// <summary>
/// `docs/08-auth-va-xavfsizlik.md` 2-bo'lim: "Minimal talab: 10 belgi, harf+raqam".
/// `public` — `AssemblyScanner` faqat public validatorlarni topadi.
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    private const int MinPasswordLength = 10;

    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Joriy parol kiritilishi shart.");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Yangi parol kiritilishi shart.")
            .MinimumLength(MinPasswordLength).WithMessage($"Yangi parol kamida {MinPasswordLength} belgidan iborat bo'lishi kerak.")
            .Must(p => p.Any(char.IsLetter)).WithMessage("Yangi parolda kamida bitta harf bo'lishi kerak.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Yangi parolda kamida bitta raqam bo'lishi kerak.");
    }
}

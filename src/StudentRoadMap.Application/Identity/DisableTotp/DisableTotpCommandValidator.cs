using FluentValidation;

namespace StudentRoadMap.Application.Identity.DisableTotp;

public sealed class DisableTotpCommandValidator : AbstractValidator<DisableTotpCommand>
{
    public DisableTotpCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Joriy parol kiritilishi shart.");
    }
}

using FluentValidation;

namespace StudentRoadMap.Application.Identity.ConfirmTotp;

public sealed class ConfirmTotpCommandValidator : AbstractValidator<ConfirmTotpCommand>
{
    public ConfirmTotpCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Tasdiqlash kodi kiritilishi shart.")
            .Matches("^[0-9]{6}$").WithMessage("Tasdiqlash kodi 6 xonali raqam bo'lishi kerak.");
    }
}

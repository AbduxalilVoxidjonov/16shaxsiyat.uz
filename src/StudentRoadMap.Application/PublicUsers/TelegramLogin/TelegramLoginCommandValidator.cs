using FluentValidation;

namespace StudentRoadMap.Application.PublicUsers.TelegramLogin;

/// <summary>
/// Faqat SHAKL tekshiruvi — haqiqiylik (`hash`) va yangilik (`auth_date`) handler'da,
/// chunki ikkalasi ham sir/vaqtga bog'liq va rad javobi bir xil generik bo'lishi kerak
/// (`LoginCommandValidator` bilan bir xil asos). `public` — `AssemblyScanner` faqat ochiq
/// validatorlarni topadi (`StartSessionCommandValidator` izohi).
/// </summary>
public sealed class TelegramLoginCommandValidator : AbstractValidator<TelegramLoginCommand>
{
    /// <summary>Telegram `hash` — SHA-256 HMAC hex ko'rinishi, aynan 64 belgi.</summary>
    private const int HashHexLength = 64;

    public TelegramLoginCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Telegram identifikatori noto'g'ri.");

        RuleFor(x => x.AuthDate)
            .GreaterThan(0).WithMessage("Telegram vaqt belgisi noto'g'ri.");

        RuleFor(x => x.Hash)
            .NotEmpty().WithMessage("Telegram imzosi yo'q.")
            .Length(HashHexLength).WithMessage("Telegram imzosi noto'g'ri formatda.")
            .Matches("^[0-9a-fA-F]+$").WithMessage("Telegram imzosi noto'g'ri formatda.");
    }
}

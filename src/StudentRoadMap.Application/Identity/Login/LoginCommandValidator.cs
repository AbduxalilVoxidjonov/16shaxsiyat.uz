using FluentValidation;

namespace StudentRoadMap.Application.Identity.Login;

/// <summary>
/// Faqat shakl tekshiruvi (bo'sh emasligi) — parol siyosati (uzunlik/murakkablik) BU YERDA
/// tekshirilmaydi, chunki login vaqtida noto'g'ri parolni ham qabul qilib, handler ichida
/// generik "Login yoki parol noto'g'ri" bilan rad etish kerak (timing-attack himoyasi,
/// `docs/13-auth-va-jwt.md` MAXSUS DIQQAT 1-band). `public` — `AssemblyScanner` faqat public
/// validatorlarni topadi (`StartSessionCommandValidator` izohiga qarang).
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Foydalanuvchi nomi kiritilishi shart.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Parol kiritilishi shart.");
    }
}

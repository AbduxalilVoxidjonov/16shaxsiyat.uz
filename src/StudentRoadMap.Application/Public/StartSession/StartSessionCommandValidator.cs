using FluentValidation;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartSession;

/// <summary>
/// `docs/07-api-shartnoma.md`/`prompts/10` talablari: FISH ≥ 5 belgi, tug'ilgan sana 6–20 yosh
/// oralig'ida, sinf 1–11, telefon `+998...`, rozilik `true`.
///
/// **`public` (`docs/06` 4-bo'lim namunasida `internal` ko'rsatilgan, lekin bu yerda ataylab
/// farq qilinadi):** `FluentValidation.DependencyInjectionExtensions` 12.1.1'dagi
/// `AssemblyScanner.FindValidatorsInAssembly` faqat OCHIQ (public) validatorlarni topadi —
/// tekshirilgan (`internal` bo'lsa `AddValidatorsFromAssembly` uni RO'YXATDAN O'TKAZMAYDI,
/// natijada validatsiya sukut bo'yicha butunlay ishlamay qoladi). Shu sabab loyihadagi barcha
/// Command/Query validator'lari `public` bo'lishi shart — handler'lar esa `internal` qolaveradi
/// (ularga tashqaridan to'g'ridan-to'g'ri murojaat qilinmaydi). PM'ga savol: `docs/06`ga shu
/// tuzatish rasman kiritilsinmi?
/// </summary>
public sealed class StartSessionCommandValidator : AbstractValidator<StartSessionCommand>
{
    private const int MinAge = 6;
    private const int MaxAge = 20;
    private const int MinFullNameLength = 5;

    public StartSessionCommandValidator(IDateTime dateTime)
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Maktab havolasi noto'g'ri.");

        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Havola tokeni noto'g'ri.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("F.I.Sh. kiritilishi shart.")
            .MinimumLength(MinFullNameLength).WithMessage($"F.I.Sh. kamida {MinFullNameLength} belgidan iborat bo'lishi kerak.");

        RuleFor(x => x.BirthDate)
            .Must(birthDate => IsAgeInRange(birthDate, dateTime.UtcNow))
            .WithMessage($"Tug'ilgan sana {MinAge}-{MaxAge} yosh oralig'iga to'g'ri kelishi kerak.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(Student.MinGrade, Student.MaxGrade)
            .WithMessage($"Sinf {Student.MinGrade}-{Student.MaxGrade} oralig'ida bo'lishi kerak.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon raqami kiritilishi shart.")
            .Must(phone => PhoneNumber.Create(phone).IsSuccess)
            .WithMessage("Telefon raqami noto'g'ri formatda (+998XXXXXXXXX).");

        RuleFor(x => x.ParentPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneNumber.Create(phone!).IsSuccess)
            .WithMessage("Ota-ona telefon raqami noto'g'ri formatda (+998XXXXXXXXX).");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email formati noto'g'ri.");

        RuleFor(x => x.ConsentAccepted)
            .Equal(true).WithMessage("Roziliksiz ro'yxatdan o'tib bo'lmaydi.");
    }

    private static bool IsAgeInRange(DateOnly birthDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (birthDate > today)
        {
            return false;
        }

        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age is >= MinAge and <= MaxAge;
    }
}

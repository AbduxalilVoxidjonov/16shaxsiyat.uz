using FluentValidation;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartPublicSession;

/// <summary>
/// `StartSessionCommandValidator` (maktab) bilan bir xil uslub, LEKIN ommaviy oqim
/// qoidalari bilan:
/// • yosh `Student.MinAge`..`Student.MaxAge` (6–99) — maktab oqimida 6–20, chunki u yerda
///   foydalanuvchi ta'rifi bo'yicha o'quvchi; bu yerda kattalar ham kiradi;
/// • sinf IXTIYORIY (`null` → `Student.NoGrade`), berilsa 1–11;
/// • 18 yoshgacha — ota-ona roziligi MAJBURIY (`docs/08` 5-bo'lim).
///
/// `public` — `AssemblyScanner` faqat ochiq validatorlarni topadi (`StartSessionCommandValidator` izohi).
/// </summary>
public sealed class StartPublicSessionCommandValidator : AbstractValidator<StartPublicSessionCommand>
{
    private const int MinFullNameLength = 5;

    public StartPublicSessionCommandValidator(IDateTime dateTime)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("F.I.Sh. kiritilishi shart.")
            .MinimumLength(MinFullNameLength).WithMessage($"F.I.Sh. kamida {MinFullNameLength} belgidan iborat bo'lishi kerak.");

        RuleFor(x => x.BirthDate)
            .Must(birthDate => Student.IsAgeAllowed(birthDate, DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime)))
            .WithMessage($"Tug'ilgan sana {Student.MinAge}-{Student.MaxAge} yosh oralig'iga to'g'ri kelishi kerak.");

        RuleFor(x => x.Grade)
            .Must(grade => grade is null || grade is >= Student.MinGrade and <= Student.MaxGrade)
            .WithMessage($"Sinf {Student.MinGrade}-{Student.MaxGrade} oralig'ida bo'lishi yoki ko'rsatilmasligi kerak.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon raqami kiritilishi shart.")
            .Must(phone => PhoneNumber.Create(phone).IsSuccess)
            .WithMessage("Telefon raqami noto'g'ri formatda (+998XXXXXXXXX).");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email formati noto'g'ri.");

        RuleFor(x => x.ConsentAccepted)
            .Equal(true).WithMessage("Roziliksiz ro'yxatdan o'tib bo'lmaydi.");

        RuleFor(x => x.ParentalConsent)
            .Equal(true)
            .When(x => IsMinor(x.BirthDate, dateTime))
            .WithMessage($"{PublicConsent.ParentalConsentRequiredBelowAge} yoshgacha bo'lganlar uchun ota-ona roziligi shart.");
    }

    private static bool IsMinor(DateOnly birthDate, IDateTime dateTime)
    {
        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);

        // Kelajakdagi sana — yosh qoidasi bu yerda tekshirilmaydi (`BirthDate` qoidasi
        // allaqachon rad etadi); ota-ona roziligi sharti esa bunday holatda qo'llanmaydi.
        return birthDate <= today
            && Student.CalculateAge(birthDate, today) < PublicConsent.ParentalConsentRequiredBelowAge;
    }
}

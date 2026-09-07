using FluentValidation;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.Common;

/// <summary>
/// Ommaviy anketa maydonlarining FORMAT qoidalari — `StartPublicSessionCommandValidator` va
/// `UpdateStudentProfileCommandValidator` uchun umumiy. Faqat maydon KELGAN bo'lsa tekshiradi;
/// MAJBURIYLIK bu yerda emas — `PublicStudentProfile.RequireFields` (handlerda, `Student`
/// topilganidan keyin; sabab `StartPublicSessionCommandValidator` izohida).
///
/// Qoidalar (maktab oqimidan farqi ataylab): yosh `Student.MinAge`..`Student.MaxAge` (6–99),
/// sinf ixtiyoriy — `0` (sinf yo'q) yoki 1–11, telefon `+998XXXXXXXXX`, email shakli.
/// </summary>
internal static class PublicProfileFormatRules
{
    public const int MinFullNameLength = 5;

    public static void AddPublicProfileFormatRules<T>(this AbstractValidator<T> validator, IDateTime dateTime)
        where T : IPublicProfileInput
    {
        validator.RuleFor(x => x.FullName)
            .MinimumLength(MinFullNameLength)
            .When(x => x.FullName is not null)
            .WithMessage($"F.I.Sh. kamida {MinFullNameLength} belgidan iborat bo'lishi kerak.");

        validator.RuleFor(x => x.BirthDate)
            .Must(birthDate => Student.IsAgeAllowed(birthDate!.Value, DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime)))
            .When(x => x.BirthDate.HasValue)
            .WithMessage($"Tug'ilgan sana {Student.MinAge}-{Student.MaxAge} yosh oralig'iga to'g'ri kelishi kerak.");

        validator.RuleFor(x => x.Grade)
            .Must(grade => grade is null || grade == Student.NoGrade || grade is >= Student.MinGrade and <= Student.MaxGrade)
            .WithMessage($"Sinf {Student.MinGrade}-{Student.MaxGrade} oralig'ida bo'lishi yoki ko'rsatilmasligi kerak.");

        validator.RuleFor(x => x.Phone)
            .Must(phone => PhoneNumber.Create(phone!).IsSuccess)
            .When(x => x.Phone is not null)
            .WithMessage("Telefon raqami noto'g'ri formatda (+998XXXXXXXXX).");

        validator.RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email formati noto'g'ri.");
    }
}

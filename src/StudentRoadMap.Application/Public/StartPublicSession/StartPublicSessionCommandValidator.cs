using FluentValidation;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartPublicSession;

/// <summary>
/// `StartSessionCommandValidator` (maktab) bilan bir xil uslub, LEKIN ommaviy oqim
/// qoidalari bilan:
/// • yosh `Student.MinAge`..`Student.MaxAge` (6–99) — maktab oqimida 6–20, chunki u yerda
///   foydalanuvchi ta'rifi bo'yicha o'quvchi; bu yerda kattalar ham kiradi;
/// • sinf IXTIYORIY (`null` → `Student.NoGrade`), berilsa `0` (sinf yo'q) yoki 1–11.
///
/// **Bu validator faqat FORMATNI tekshiradi, MAJBURIYLIKNI emas.** 2026-09-07 dan
/// shaxsiy maydonlar ixtiyoriy: mavjud `Student` bo'lsa ular qayta so'ralmaydi. "Qaysi
/// maydon shart" javobi `Student` bazada bor-yo'qligiga (va `ConsentVersion` eskirganiga)
/// bog'liq — validator esa pipeline'da handlerdan OLDIN turadi va DB ko'rmaydi
/// (`ValidationBehavior`). DB'ga murojaat qiluvchi validator ikki marta so'rov yuborardi
/// va "tekshiruv chegarada, biznes handlerda" (`docs/06`) chizig'ini buzardi. Shu sabab:
/// • FORMAT (uzunlik, yosh oralig'i, telefon/email shakli, sinf oralig'i) — bu yerda,
///   faqat maydon KELGAN bo'lsa;
/// • MAJBURIYLIK (yangi profilda to'liq to'plam, eskirgan rozilik, ota-ona roziligi) —
///   `StartPublicSessionCommandHandler`, `Student` topilganidan keyin, o'sha
///   `400 VALIDATION_ERROR` + `errors{maydon: [xabar]}` shaklida.
///
/// `public` — `AssemblyScanner` faqat ochiq validatorlarni topadi (`StartSessionCommandValidator` izohi).
/// </summary>
public sealed class StartPublicSessionCommandValidator : AbstractValidator<StartPublicSessionCommand>
{
    public const int MinFullNameLength = 5;

    public StartPublicSessionCommandValidator(IDateTime dateTime)
    {
        RuleFor(x => x.FullName)
            .MinimumLength(MinFullNameLength)
            .When(x => x.FullName is not null)
            .WithMessage($"F.I.Sh. kamida {MinFullNameLength} belgidan iborat bo'lishi kerak.");

        RuleFor(x => x.BirthDate)
            .Must(birthDate => Student.IsAgeAllowed(birthDate!.Value, DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime)))
            .When(x => x.BirthDate.HasValue)
            .WithMessage($"Tug'ilgan sana {Student.MinAge}-{Student.MaxAge} yosh oralig'iga to'g'ri kelishi kerak.");

        RuleFor(x => x.Grade)
            .Must(grade => grade is null || grade == Student.NoGrade || grade is >= Student.MinGrade and <= Student.MaxGrade)
            .WithMessage($"Sinf {Student.MinGrade}-{Student.MaxGrade} oralig'ida bo'lishi yoki ko'rsatilmasligi kerak.");

        RuleFor(x => x.Phone)
            .Must(phone => PhoneNumber.Create(phone!).IsSuccess)
            .When(x => x.Phone is not null)
            .WithMessage("Telefon raqami noto'g'ri formatda (+998XXXXXXXXX).");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email formati noto'g'ri.");
    }
}

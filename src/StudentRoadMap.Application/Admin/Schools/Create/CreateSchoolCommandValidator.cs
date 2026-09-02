using FluentValidation;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Schools.Create;

/// <summary>
/// `docs/04-domain-model.md` 2.1-bo'lim maydon uzunliklari. **`public`** — `FluentValidation`
/// `AssemblyScanner` faqat ochiq validatorlarni topadi (`StartSessionCommandValidator` izohiga qarang).
/// </summary>
public sealed class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
{
    public CreateSchoolCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Maktab nomi kiritilishi shart.")
            .MaximumLength(200);

        RuleFor(x => x.Region)
            .NotEmpty().WithMessage("Viloyat kiritilishi shart.")
            .MaximumLength(100);

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("Tuman/shahar kiritilishi shart.")
            .MaximumLength(100);

        RuleFor(x => x.SchoolNumber).MaximumLength(20);
        RuleFor(x => x.ContactPerson).MaximumLength(150);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x.AccessCode)
            .Matches("^[0-9]{6}$")
            .When(x => !string.IsNullOrEmpty(x.AccessCode))
            .WithMessage("Kirish kodi 6 ta raqamdan iborat bo'lishi kerak.");

        RuleFor(x => x.DailyRegistrationLimit)
            .GreaterThan(0)
            .When(x => x.DailyRegistrationLimit.HasValue)
            .WithMessage("Kunlik ro'yxatdan o'tish limiti musbat bo'lishi kerak.");

        // `SchoolSlug.Create` o'zi ham tekshiradi (bo'sh natija), lekin bu yerda ERTAROQ,
        // aniqroq xabar bilan qaytariladi — `Name`/`District` allaqachon yuqorida `NotEmpty`.
        RuleFor(x => x)
            .Must(x => SchoolSlug.Create($"{x.Name} {x.District}").IsSuccess)
            .WithName("Name")
            .WithMessage("Maktab nomi va tumandan yaroqli havola manzili (slug) hosil bo'lmadi.");
    }
}

using FluentValidation;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `public` — `AssemblyScanner` faqat ochiq validatorlarni topadi. Sahifalash tekshirilMAYDI
/// (`AdminPagingOptions.Normalize` jimgina to'g'irlaydi). `status`/`activityLevel` noma'lum
/// bo'lsa filtr jimgina qo'llanmaydi (tarixiy xatti-harakat, `AdminStudentFilterBuilder`);
/// `gender`/`ageMin`/`ageMax` esa YANGI (2026-09-07) va noto'g'ri qiymatda 400 qaytaradi —
/// `ageMin > ageMax` bo'sh ro'yxat sifatida "hech kim topilmadi" degan yolg'on javob bermasin.
/// Qoidalar `AdminStudentFilterRules` da (eksport validatori bilan bitta manba).
/// </summary>
public sealed class ListStudentsQueryValidator : AbstractValidator<ListStudentsQuery>
{
    public ListStudentsQueryValidator()
    {
        RuleFor(x => x.Gender)
            .Must(AdminStudentFilterRules.IsValidGender)
            .WithMessage(AdminStudentFilterRules.GenderMessage);

        RuleFor(x => x.AgeMin)
            .Must(AdminStudentFilterRules.IsValidAge)
            .WithMessage(AdminStudentFilterRules.AgeRangeMessage);

        RuleFor(x => x.AgeMax)
            .Must(AdminStudentFilterRules.IsValidAge)
            .WithMessage(AdminStudentFilterRules.AgeRangeMessage);

        RuleFor(x => x)
            .Must(x => AdminStudentFilterRules.IsValidAgeOrder(x.AgeMin, x.AgeMax))
            .WithName(nameof(ListStudentsQuery.AgeMin))
            .WithMessage(AdminStudentFilterRules.AgeOrderMessage);
    }
}

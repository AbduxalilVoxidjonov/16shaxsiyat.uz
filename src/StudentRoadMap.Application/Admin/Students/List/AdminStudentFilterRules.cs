using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// `gender`/`ageMin`/`ageMax` filtr parametrlarining validatsiya qoidalari — `ListStudentsQueryValidator`
/// va `ExportStudentsQueryValidator` IKKALASI ham shu bitta manbadan oladi (ro'yxat qabul
/// qilgan filtrni eksport rad etmasin va aksincha). Yosh chegarasi `Student.MinAge..MaxAge`
/// (6–99) — anketa qabul qiladigan oraliq bilan bir xil, undan tashqaridagi filtr ma'nosiz.
/// </summary>
internal static class AdminStudentFilterRules
{
    public const int MinAge = Student.MinAge;
    public const int MaxAge = Student.MaxAge;

    public static readonly string GenderMessage = "Jins filtri `Male` yoki `Female` bo'lishi kerak.";
    public static readonly string AgeRangeMessage = $"Yosh {MinAge} dan {MaxAge} gacha bo'lishi kerak.";
    public const string AgeOrderMessage = "`ageMin` `ageMax` dan katta bo'lishi mumkin emas.";

    /// <summary>
    /// Bo'sh — filtr yo'q (to'g'ri); aks holda faqat `Male`/`Female` NOMI (katta-kichik harf
    /// farqsiz). Raqamli qiymat (`"1"`) qabul qilinmaydi — `Enum.TryParse` uni jimgina `Male`ga
    /// aylantirardi, API esa enum'ni faqat string nomi bilan qabul qiladi (`docs/07` 4-bo'lim).
    /// </summary>
    public static bool IsValidGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return true;
        }

        var trimmed = gender.Trim();
        return string.Equals(trimmed, nameof(Gender.Male), StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, nameof(Gender.Female), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidAge(int? age) => age is null || (age >= MinAge && age <= MaxAge);

    public static bool IsValidAgeOrder(int? ageMin, int? ageMax) =>
        ageMin is null || ageMax is null || ageMin <= ageMax;
}

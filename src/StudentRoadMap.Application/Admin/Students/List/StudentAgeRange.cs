namespace StudentRoadMap.Application.Admin.Students.List;

/// <summary>
/// Yosh oralig'i (`?ageMin=&amp;ageMax=`) → `BirthDate` chegaralari (`docs/07` 3.2-bo'lim).
/// Filtr DB darajasida `students.birth_date` ustuniga qo'llanadi (`ix_students_*`
/// indekslariga mos, yosh hisoblash uchun qatorlar xotiraga olinmaydi). Bugungi sana
/// PARAMETR bilan keladi (`IDateTime`, `CLAUDE.md` 2-qoida — `DateTime.Now` yo'q).
///
/// <para>
/// Formulalar (`Student.CalculateAge` bilan aynan mos — to'liq yosh, tug'ilgan kun o'tganini
/// hisobga oladi):
/// <list type="bullet">
/// <item><c>age &gt;= min</c> ⇔ <c>birthDate &lt;= today − min yil</c> (bugun tug'ilgan kuni
/// bo'lgan o'quvchi `min` yoshga TO'LGAN — kiradi).</item>
/// <item><c>age &lt;= max</c> ⇔ <c>birthDate &gt; today − (max + 1) yil</c> (bugun `max + 1`
/// yoshga to'lgan o'quvchi ENDI kirmaydi).</item>
/// </list>
/// `DateOnly.AddYears` 29-fevralni kabisa bo'lmagan yilda 28-fevralga tushiradi — bu
/// `CalculateAge` xatti-harakati bilan bir xil (testlarda qulflangan).
/// </para>
/// </summary>
internal static class StudentAgeRange
{
    /// <summary>`age &gt;= ageMin` uchun eng KECHKI tug'ilgan sana (inklyuziv: `birthDate &lt;= natija`).</summary>
    public static DateOnly LatestBirthDateInclusive(int ageMin, DateOnly today) => today.AddYears(-ageMin);

    /// <summary>`age &lt;= ageMax` uchun eng ERTA tug'ilgan sana (EKSKLYUZIV: `birthDate &gt; natija`).</summary>
    public static DateOnly EarliestBirthDateExclusive(int ageMax, DateOnly today) => today.AddYears(-(ageMax + 1));
}

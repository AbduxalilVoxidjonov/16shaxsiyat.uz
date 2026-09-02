using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Admin.Common;

/// <summary>
/// Domain enum'larini foydalanuvchiga ko'rinadigan o'zbekcha matnga o'giradi (`CLAUDE.md` 1-band).
/// Faqat eksport (`prompts/27`) uchun — API JSON javoblari xom (inglizcha) enum nomini
/// qaytarishda davom etadi (`docs/07` shakli, frontend o'zi tarjima qiladi); Excel/PDF esa
/// server tomonida generatsiya qilingan tayyor fayl bo'lgani uchun matn shu yerda tarjima
/// qilinadi — `Application.Admin.Students.Export`/`Application.Admin.Assessments.Export`
/// ikkalasi ham qayta ishlatadi (bitta manba, ikki joyda takrorlanmasin).
/// </summary>
internal static class AdminEnumLabels
{
    public static string Gender(Gender gender) => gender switch
    {
        Domain.Students.Gender.Male => "Erkak",
        Domain.Students.Gender.Female => "Ayol",
        _ => "Ko'rsatilmagan",
    };

    public static string ActivityLevel(ActivityLevel level) => level switch
    {
        Domain.Students.ActivityLevel.Passive => "Passiv",
        Domain.Students.ActivityLevel.LowActive => "Kam faol",
        Domain.Students.ActivityLevel.Moderate => "O'rtacha faol",
        Domain.Students.ActivityLevel.Active => "Faol",
        Domain.Students.ActivityLevel.HighlyActive => "Juda faol",
        _ => level.ToString(),
    };

    public static string ReliabilityFlag(ReliabilityFlag flag) => flag switch
    {
        Domain.Assessments.ReliabilityFlag.Reliable => "Ishonchli",
        Domain.Assessments.ReliabilityFlag.Questionable => "Shubhali",
        Domain.Assessments.ReliabilityFlag.Unreliable => "Ishonchsiz",
        _ => flag.ToString(),
    };

    public static string AssessmentStatus(AssessmentStatus status) => status switch
    {
        Domain.Assessments.AssessmentStatus.Draft => "Boshlanmagan",
        Domain.Assessments.AssessmentStatus.InProgress => "Jarayonda",
        Domain.Assessments.AssessmentStatus.Completed => "Yakunlangan",
        Domain.Assessments.AssessmentStatus.Analyzing => "Tahlil qilinmoqda",
        Domain.Assessments.AssessmentStatus.Analyzed => "Tahlil qilingan",
        Domain.Assessments.AssessmentStatus.AnalysisFailed => "Tahlil xato bo'ldi",
        Domain.Assessments.AssessmentStatus.Abandoned => "Tashlab ketilgan",
        _ => status.ToString(),
    };
}

using StudentRoadMap.Application.Public.StartPublicSession;
using StudentRoadMap.Domain.PublicUsers;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.GetStudentProfile;

/// <summary>
/// `Student` (+ `PublicUser`) → <see cref="MyStudentProfileDto"/>. `GET /api/me/profile` va
/// `PUT /api/me/profile` javobi BIR XIL shaklda bo'lishi shart (frontend ikkalasini bitta
/// `MyStudentProfile` tipiga o'qiydi va saqlashdan keyin keshni javob bilan almashtiradi) —
/// shu sabab xaritalash ikki handlerda emas, bitta joyda.
/// </summary>
internal static class MyStudentProfileMapper
{
    public static MyStudentProfileDto ToDto(PublicUser user, Student? student, DateOnly today)
    {
        var suggestedFullName = SuggestFullName(user);

        if (student is null)
        {
            return new MyStudentProfileDto(
                HasProfile: false,
                FullName: null,
                BirthDate: null,
                Gender: null,
                Phone: null,
                Grade: null,
                Email: null,
                ConsentVersion: null,
                ConsentCurrent: false,
                ParentalConsent: false,
                IsMinor: false,
                SuggestedFullName: suggestedFullName);
        }

        var isMinor = student.BirthDate <= today
            && Student.CalculateAge(student.BirthDate, today) < PublicConsent.ParentalConsentRequiredBelowAge;

        return new MyStudentProfileDto(
            HasProfile: true,
            FullName: student.FullName,
            BirthDate: student.BirthDate,
            Gender: student.Gender,
            Phone: student.Phone.Value,
            Grade: student.Grade == Student.NoGrade ? null : student.Grade,
            Email: student.Email,
            ConsentVersion: student.ConsentVersion,
            ConsentCurrent: student.ConsentVersion == PublicConsent.CurrentVersion,
            ParentalConsent: student.ParentalConsent,
            IsMinor: isMinor,
            SuggestedFullName: suggestedFullName);
    }

    /// <summary>
    /// Telegram ismidan F.I.Sh. TAKLIFI: `Familiya Ism` tartibida (o'zbek rasmiy yozuvi);
    /// biri yo'q bo'lsa bori; ikkalasi yo'q — `null`. Bu qiymat AI'ga hech qachon
    /// tushmaydi (`CLAUDE.md` 5-qoida) — faqat anketa maydonini oldindan to'ldirish uchun.
    /// </summary>
    private static string? SuggestFullName(PublicUser user)
    {
        var parts = new[] { user.LastName, user.FirstName }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim());

        var joined = string.Join(' ', parts);
        return joined.Length == 0 ? null : joined;
    }
}

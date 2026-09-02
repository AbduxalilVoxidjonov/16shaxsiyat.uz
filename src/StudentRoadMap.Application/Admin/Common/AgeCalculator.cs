namespace StudentRoadMap.Application.Admin.Common;

/// <summary>
/// `GetStudentByIdQueryHandler.CalculateAge`dagi bilan bir xil hisob-kitob (`docs/07` 3.2
/// `age` maydoni) — eksport handler'lari (`prompts/27`, `Admin/Students/Export`,
/// `Admin/Assessments/Export`) uchun bitta joyda. `GetStudentByIdQueryHandler` o'z private
/// nusxasini saqlab qoladi (P14 hududi, bu yerdan qasddan tegilmadi).
/// </summary>
internal static class AgeCalculator
{
    public static int CalculateAge(DateOnly birthDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}

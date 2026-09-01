namespace StudentRoadMap.Application.Public.Common;

/// <summary>
/// O'quvchining to'liq ismini ommaviy javoblarda ko'rsatib bo'lmaydigan qismlardan tozalaydi
/// (`prompts/10-ommaviy-sessiya-api.md` cheklovi: "Javobda o'quvchining to'liq ismi yo'q —
/// faqat ism, `firstNameShort`"). F.I.Sh. formati `"Familiya Ism Otasining ismi"`
/// (`docs/07` 1.2-bo'lim namunasi: `"Aliyev Sardor Bekzodovich"` → `"Sardor"`), shu sabab
/// ikkinchi so'z olinadi; format kutilganidan farq qilsa (bitta/uchtadan ortiq so'z emas)
/// birinchi so'zga tushiladi.
/// </summary>
public static class StudentNameHelper
{
    public static string ExtractFirstNameShort(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length >= 2 ? parts[1] : parts[0];
    }
}

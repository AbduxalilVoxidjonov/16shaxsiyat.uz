namespace StudentRoadMap.Application.Admin.Common;

/// <summary>
/// `sort` so'rov parametrini (`docs/07` 4-bo'lim: `name`, `-createdAt` — minus = kamayish)
/// maydon nomi + yo'nalishga ajratadi. Haqiqiy `OrderBy` chaqiruvi har handler ICHIDA, qat'iy
/// oq ro'yxat (`switch`) bilan qilinadi — `prompts/14` MAXSUS DIQQAT #1: foydalanuvchi matni
/// to'g'ridan-to'g'ri `OrderBy`ga ketmaydi (SQL injection / kutilmagan ustunga tartiblash
/// xavfi). Noma'lum maydon — jimgina standart maydonga tushadi (xato emas).
/// </summary>
internal static class AdminSortSpec
{
    public static (string Field, bool Descending) Parse(string? sort, string defaultField)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return (defaultField, false);
        }

        var trimmed = sort.Trim();
        var descending = trimmed.StartsWith('-');
        var field = descending ? trimmed[1..] : trimmed;

        return string.IsNullOrWhiteSpace(field) ? (defaultField, false) : (field, descending);
    }
}

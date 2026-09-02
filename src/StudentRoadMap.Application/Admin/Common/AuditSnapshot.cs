using System.Text.Json;

namespace StudentRoadMap.Application.Admin.Common;

/// <summary>
/// `AuditLog.BeforeJson`/`AfterJson` uchun umumiy serializatsiya — chaqiruvchi (har handler)
/// ANIQ qaysi maydonlar ketishini o'zi tanlaydi (anonim obyekt), shu bilan `CLAUDE.md` 6-band /
/// `prompts/14` MAXSUS DIQQAT #6 talabi ta'minlanadi: shaxsiy ma'lumot (o'quvchi ismi, telefoni)
/// audit'ga HECH QACHON to'liq ko'chirilmaydi — faqat ID va o'zgargan maydon nomlari/qiymatlari
/// (masalan `Student.Deleted`da faqat `{studentId, deletedAt}`).
/// </summary>
internal static class AuditSnapshot
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Serialize(object snapshot) => JsonSerializer.Serialize(snapshot, Options);
}

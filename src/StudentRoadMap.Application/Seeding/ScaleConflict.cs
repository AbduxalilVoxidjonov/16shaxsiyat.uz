namespace StudentRoadMap.Application.Seeding;

/// <summary>
/// Bazadagi mavjud savol bilan seed faylidagi savol o'rtasida `Scale`/`Direction`/`Weight` farqi
/// (BR-8 buzilishi — tizim metodikasi shkalasi qulflangan, `CLAUDE.md` 9a-qoida: "Scale/Direction/
/// Weight o'zgarmaydi"). `DbSeeder` bu ro'yxat bo'sh bo'lmasa xato bilan to'xtaydi va har bir
/// farqni log qiladi.
/// </summary>
public sealed record ScaleConflict(
    string QuestionCode,
    string ExistingScale,
    int ExistingDirection,
    decimal ExistingWeight,
    string IncomingScale,
    int IncomingDirection,
    decimal IncomingWeight);

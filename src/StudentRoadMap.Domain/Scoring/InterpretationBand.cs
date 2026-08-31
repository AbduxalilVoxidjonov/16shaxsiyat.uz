namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Bitta shkala uchun talqin oralig'i — superadmin `Custom` anketa yaratganda belgilaydi
/// (`docs/03` §6.1, `docs/04` §2.7 `TestScale.InterpretationBandsJson`; `TestScale` entity
/// hali P33'da qo'shiladi — hozircha bu qiymat chaqiruvchi tomonidan uzatiladi).
/// `MinInclusive`/`MaxInclusive` — 0..100 oralig'ida, bo'shliqsiz va ustma-ust tushmasdan
/// (nashr validatsiyasi `docs/03` §6.3, Application qatlamida amalga oshiriladi).
/// </summary>
/// <param name="MinInclusive">Oraliqning pastki chegarasi (kiritilgan).</param>
/// <param name="MaxInclusive">Oraliqning yuqori chegarasi (kiritilgan).</param>
/// <param name="Level">Daraja nomi (masalan, "Past", "O'rtacha", "Yuqori").</param>
public sealed record InterpretationBand(double MinInclusive, double MaxInclusive, string Level);

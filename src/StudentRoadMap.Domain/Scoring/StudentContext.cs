using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// AI'ga uzatiladigan shaxsiy bo'lmagan o'quvchi konteksti (`docs/06` §8: "yosh, sinf, jins").
/// Hozirgi (P09) strategiyalar formulalarida ishlatilmaydi — kelajakda yosh/sinf bo'yicha
/// normalizatsiya kerak bo'lsa, kontrakt allaqachon tayyor bo'lishi uchun kiritilgan.
/// `CLAUDE.md` qat'iy qoida 5: ism, telefon, email, tug'ilgan sana, maktab nomi — bu yerda yo'q.
/// </summary>
/// <param name="Age">O'quvchi yoshi (to'liq yil).</param>
/// <param name="Grade">Sinf (1..11).</param>
/// <param name="Gender">Jins.</param>
public sealed record StudentContext(int? Age, int? Grade, Gender? Gender);

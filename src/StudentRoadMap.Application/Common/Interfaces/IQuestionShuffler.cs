namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Savollar tartibini aralashtirish abstraksiyasi — `prompts/11` talabi: "Random"ni
/// `Domain`da to'g'ridan-to'g'ri ishlatib bo'lmaydi (`CLAUDE.md` 2-qoida), `Application`da esa
/// test qilinadigan bo'lishi uchun bevosita `System.Random` emas, shu abstraksiya orqali
/// chaqiriladi (`Infrastructure`da `RandomQuestionShuffler` bilan amalga oshiriladi).
/// </summary>
public interface IQuestionShuffler
{
    /// <summary>Berilgan ID ro'yxatining YANGI, aralashtirilgan nusxasini qaytaradi (kirish ro'yxati o'zgarmaydi).</summary>
    IReadOnlyList<Guid> Shuffle(IReadOnlyList<Guid> questionIds);
}

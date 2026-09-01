using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>
/// `IQuestionShuffler`ning haqiqiy (kriptografik emas — UI tartibi uchun yetarli) tasodifiy
/// amalga oshirilishi. Fisher-Yates algoritmi (`docs`da aniq algoritm ko'rsatilmagan — PM'ga
/// savol: kriptografik `RandomNumberGenerator` talab qilinadimi, yoki `System.Random` yetarlimi?
/// Hozircha `System.Random.Shared` — tartib xavfsizlik uchun emas, faqat UI monotonlik uchun).
/// </summary>
public sealed class RandomQuestionShuffler : IQuestionShuffler
{
    public IReadOnlyList<Guid> Shuffle(IReadOnlyList<Guid> questionIds)
    {
        var result = questionIds.ToList();
        var random = Random.Shared;

        for (var i = result.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}

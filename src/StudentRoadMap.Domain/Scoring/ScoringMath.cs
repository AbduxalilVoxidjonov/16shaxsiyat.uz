using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Barcha strategiyalar uchun umumiy sof yordamchi funksiyalar. Bitta strategiya boshqasini
/// chaqirmaydi degan qoida (`prompts/09-scoring-engine.md`) shu yordamchiga taalluqli emas —
/// bu strategiyalararo emas, umumiy matematik yordamchi.
/// </summary>
internal static class ScoringMath
{
    /// <summary>
    /// Savolning xom javobini oladi va `[min,max]` oralig'ida ekanligini tekshiradi. Javob yo'q
    /// bo'lsa (majburiy savolga javob berilmagan) yoki qiymat savol turining diapazonidan tashqarida
    /// bo'lsa (masalan `Likert5` savoliga `9`) aniq xato bilan to'xtaydi — bu ommaviy API'dan kelgan
    /// buzilgan ma'lumotga qarshi so'nggi mudofaa (Application qatlami buni oldindan tekshirishi
    /// kerak, `docs/12-testlash-strategiyasi.md` §4, lekin Scoring o'ziga ishonib qolmaydi).
    /// </summary>
    public static int GetRequiredAnswer(IReadOnlyDictionary<Guid, int> answers, QuestionMeta question, int min, int max)
    {
        if (!answers.TryGetValue(question.QuestionId, out var value))
        {
            throw new DomainException(
                "SCORING_MISSING_ANSWER",
                $"'{question.Code}' ({question.Scale}) savoliga javob topilmadi — scoring to'liq javoblar to'plamini talab qiladi.");
        }

        if (value < min || value > max)
        {
            throw new DomainException(
                "SCORING_ANSWER_OUT_OF_RANGE",
                $"'{question.Code}' javobi [{min},{max}] oralig'idan tashqarida: {value}.");
        }

        return value;
    }

    /// <summary>
    /// Tizim metodikalarida (`MBTI16`/`BIG5`/`RIASEC`/`ACTIVITY`) `Weight` hisobga olinmaydi —
    /// formulalar `docs/03` da og'irliksiz yozilgan, faqat `SUM` (ADR-13) og'irlik ishlatadi.
    /// `IsSystem` savollarida `Weight` har doim `1.0` bo'lishi kerak (BR-8); boshqacha qiymat
    /// kelsa jimgina e'tiborsiz qoldirilmaydi — aniq xato beriladi.
    /// </summary>
    public static void EnsureUnitWeight(QuestionMeta question)
    {
        if (question.Weight != 1.0m)
        {
            throw new DomainException(
                "SCORING_UNEXPECTED_WEIGHT",
                $"'{question.Code}' savolining og'irligi 1.0 bo'lishi kerak (bu strategiya `Weight`ni hisobga olmaydi), berilgan: {question.Weight}.");
        }
    }

    /// <summary>Teskari savol formulasi: `v' = (max + min) − v` (`docs/03` §1). To'g'ri yo'nalishda o'zgarmaydi.</summary>
    public static int ApplyDirection(int rawValue, int direction, int min, int max) =>
        direction > 0 ? rawValue : max + min - rawValue;

    /// <summary>Savollarni shkala bo'yicha guruhlaydi, har guruh ichida `DisplayOrder` bo'yicha
    /// tartiblaydi — natija kirish ro'yxatining tasodifiy tartibiga bog'liq emas.</summary>
    public static IReadOnlyDictionary<string, List<QuestionMeta>> GroupByScaleOrdered(IReadOnlyList<QuestionMeta> questions)
    {
        return questions
            .GroupBy(q => q.Scale, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(q => q.DisplayOrder).ThenBy(q => q.QuestionId).ToList(),
                StringComparer.Ordinal);
    }

    /// <summary>Javob turi bo'yicha `(min, max)` diapazoni — faqat `SUM` va ishonchlilik hisobida
    /// ishlatiladi (`docs/03` §6.1: `Likert5` yoki `Likert7`).</summary>
    public static (int Min, int Max) GetLikertBounds(QuestionType questionType) => questionType switch
    {
        QuestionType.Likert7 => (ScoringConstants.Likert7Min, ScoringConstants.Likert7Max),
        _ => (ScoringConstants.Likert5Min, ScoringConstants.Likert5Max),
    };
}

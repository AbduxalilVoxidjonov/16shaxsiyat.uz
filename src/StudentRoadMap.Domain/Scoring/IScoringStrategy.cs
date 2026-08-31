namespace StudentRoadMap.Domain.Scoring;

/// <summary>
/// Bitta metodika (test) uchun ball hisoblash strategiyasi — `docs/06-arxitektura.md` §8.
/// Sof funksiya: DB'ga murojaat qilmaydi, IO yo'q, tizim soatidan foydalanmaydi, tashqi holat yo'q.
/// Bir xil <see cref="ScoringInput"/> har doim bir xil <see cref="ScoringResult"/> beradi.
/// </summary>
public interface IScoringStrategy
{
    /// <summary>`TestDefinition.ScoringStrategyCode` bilan mos keladigan identifikator
    /// (`"MBTI16"`, `"BIG5"`, `"RIASEC"`, `"ACTIVITY"`, `"SUM"`).</summary>
    string StrategyCode { get; }

    /// <summary>
    /// Kirishni deterministik tarzda ballga aylantiradi. Kirish nomuvofiq bo'lsa
    /// (masalan majburiy savolga javob yo'q) <see cref="Common.DomainException"/> otiladi —
    /// bu holat Application qatlamida oldindan (barcha majburiy savol to'ldirilgach) oldi
    /// olinishi kerak bo'lgan kontrakt buzilishi, oddiy biznes oqimi emas.
    /// </summary>
    ScoringResult Score(ScoringInput input);
}

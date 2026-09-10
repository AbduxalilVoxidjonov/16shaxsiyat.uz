namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// Bitta ko'rsatish shartining solishtirish operatori (`docs/18` §2.4). Baholash qoidalari
/// `VisibilityEvaluator`da — javobsiz holatda `Equals`/`AnyOf`/`ContainsAny`/`ContainsAll`/
/// `NotEquals`/`NoneOf` HAMMASI `false` qaytaradi (B-4 bilan bog'liq, javob berilmagan
/// savolga tayangan tarmoq ochilib ketmasligi uchun).
/// </summary>
public enum VisibilityOperator
{
    /// <summary>Javob qiymati (yoki yagona tanlangan `MultiChoice` qiymati) `values[0]`ga teng.</summary>
    Equals = 1,

    /// <summary>`Equals`ning teskarisi — javob yo'q bo'lsa ham `false` (B-4 izohiga qarang).</summary>
    NotEquals = 2,

    /// <summary>Javob qiymati `values` ichida.</summary>
    AnyOf = 3,

    /// <summary>`AnyOf`ning teskarisi — javob yo'q bo'lsa ham `false`.</summary>
    NoneOf = 4,

    /// <summary>`MultiChoice`: tanlangan qiymatlar `values` bilan kamida bitta umumiy elementga ega.</summary>
    ContainsAny = 5,

    /// <summary>`MultiChoice`: `values`ning barchasi tanlangan.</summary>
    ContainsAll = 6,

    /// <summary>Savolga javob berilgan (`AnswerSnapshot.IsAnswered`). `values` bo'sh bo'lishi shart.</summary>
    Answered = 7,

    /// <summary>Savolga javob berilmagan. `values` bo'sh bo'lishi shart.</summary>
    NotAnswered = 8,
}

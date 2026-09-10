namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// `VisibilityEvaluator`/`VisibleQuestionResolver` kirishi uchun bitta savolning joriy javobi
/// (xotirada, DB'siz) — `docs/18` §2.5. Uchta maydondan faqat mos keladigani to'ldiriladi
/// (`Answer` shakl invariantiga mos, lekin bu yerda qat'iy tekshiruv YO'Q — chaqiruvchi
/// tozalangan ma'lumot uzatadi).
/// </summary>
public sealed record AnswerSnapshot(int? RawValue, string? TextValue, IReadOnlyList<int> SelectedValues)
{
    /// <summary>Savolga birror shaklda javob berilganmi (bo'sh/whitespace matn — javobsiz hisoblanadi).</summary>
    public bool IsAnswered => RawValue is not null
        || !string.IsNullOrWhiteSpace(TextValue)
        || SelectedValues.Count > 0;
}

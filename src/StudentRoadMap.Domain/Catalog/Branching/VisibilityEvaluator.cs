namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// Bitta ko'rsatish qoidasini baholaydi — sof, deterministik, qatlamsiz (`docs/18` §2.5).
/// Kaskad/bo'lim ustunligi mantig'i bu yerda YO'Q — u `VisibleQuestionResolver`da.
/// </summary>
public static class VisibilityEvaluator
{
    /// <summary>`rule is null` bo'lsa har doim `true` (shart yo'q — savol/bo'lim shartsiz ko'rinadi).</summary>
    public static bool Evaluate(VisibilityRule? rule, IReadOnlyDictionary<string, AnswerSnapshot> answers)
    {
        ArgumentNullException.ThrowIfNull(answers);

        if (rule is null)
        {
            return true;
        }

        return rule.Match switch
        {
            VisibilityMatch.All => rule.Conditions.All(condition => EvaluateCondition(condition, answers)),
            VisibilityMatch.Any => rule.Conditions.Any(condition => EvaluateCondition(condition, answers)),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Match, "Noma'lum VisibilityMatch qiymati."),
        };
    }

    private static bool EvaluateCondition(VisibilityCondition condition, IReadOnlyDictionary<string, AnswerSnapshot> answers)
    {
        answers.TryGetValue(condition.QuestionCode, out var snapshot);

        // MAVJUDLIK emas, JAVOB BORLIGI tekshiriladi. `Answer` yozuvi bazada turgan holda ham
        // bo'sh bo'lishi mumkin (`MultiChoice` da tanlov olib tashlangan, matn faqat bo'shliqqa
        // aylangan) — `snapshot is not null` deb tekshirilsa, bunday javob "boshqa qiymat" deb
        // hisoblanib, `NotEquals`/`NoneOf` tarmoqlari o'z-o'zidan ochilib ketardi. Bu — oltin
        // fikstura ("MultiChoice manba + NotEquals — tanlov bo'sh bo'lsa (javobsiz) FALSE")
        // ushlagan xato; TS egizagi (`shared/lib/visibility.ts`) boshidan `isAnswered` bo'yicha
        // ishlagan, ya'ni ikki nusxa aynan shu joyda ajralib ketgan edi.
        var answered = snapshot?.IsAnswered ?? false;

        return condition.Operator switch
        {
            VisibilityOperator.Answered => answered,
            VisibilityOperator.NotAnswered => !answered,

            // `docs/18` §2.5 jadvali: javob yo'q bo'lsa HAMMASI `false` (NotEquals/NoneOf ham) —
            // "hali javob bermagan" holat "boshqa qiymat" deb hisoblanmasin.
            VisibilityOperator.Equals => answered && MatchesEquals(snapshot!, condition.Values),
            VisibilityOperator.NotEquals => answered && !MatchesEquals(snapshot!, condition.Values),
            VisibilityOperator.AnyOf => answered && MatchesAnyOf(snapshot!, condition.Values),
            VisibilityOperator.NoneOf => answered && !MatchesAnyOf(snapshot!, condition.Values),
            VisibilityOperator.ContainsAny => answered && snapshot!.SelectedValues.Intersect(condition.Values).Any(),
            VisibilityOperator.ContainsAll => answered && condition.Values.All(v => snapshot!.SelectedValues.Contains(v)),
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Operator, "Noma'lum VisibilityOperator qiymati."),
        };
    }

    /// <summary>
    /// `Equals`/`NotEquals` — `RawValue` bo'yicha. Manba savol `MultiChoice` bo'lsa (`RawValue`
    /// yo'q, `SelectedValues` bor), `Equals` "yagona tanlov shu qiymatga teng" ma'nosida
    /// ishlaydi (`docs/18` §2.5).
    /// </summary>
    private static bool MatchesEquals(AnswerSnapshot snapshot, IReadOnlyList<int> values)
    {
        if (snapshot.SelectedValues.Count > 0)
        {
            return snapshot.SelectedValues.Count == 1 && snapshot.SelectedValues[0] == values[0];
        }

        return snapshot.RawValue is not null && snapshot.RawValue.Value == values[0];
    }

    /// <summary>`AnyOf`/`NoneOf` — `MatchesEquals`dagi kabi, lekin ro'yxat bilan.</summary>
    private static bool MatchesAnyOf(AnswerSnapshot snapshot, IReadOnlyList<int> values)
    {
        if (snapshot.SelectedValues.Count > 0)
        {
            return snapshot.SelectedValues.Any(values.Contains);
        }

        return snapshot.RawValue is not null && values.Contains(snapshot.RawValue.Value);
    }
}

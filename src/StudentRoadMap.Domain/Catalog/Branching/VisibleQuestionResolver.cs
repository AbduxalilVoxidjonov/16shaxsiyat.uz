namespace StudentRoadMap.Domain.Catalog.Branching;

/// <summary>
/// Butun anketa uchun ko'rinadigan savollar/bo'limlar to'plamini **bir marta o'tishda**
/// hisoblaydi (`docs/18` §2.6). Sof, deterministik, qatlamsiz.
/// </summary>
public static class VisibleQuestionResolver
{
    public static VisibilityMap Resolve(
        IReadOnlyList<SectionSnapshot> sections,
        IReadOnlyList<QuestionSnapshot> questions,
        IReadOnlyDictionary<string, AnswerSnapshot> answersByQuestionCode)
    {
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answersByQuestionCode);

        var sectionsById = sections.ToDictionary(s => s.Id);
        var orderedQuestions = questions.OrderBy(q => q.DisplayOrder).ThenBy(q => q.Id).ToList();
        var orderedSections = sections.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id).ToList();

        // Ishchi (mutatsiyalanadigan) javoblar nusxasi — kaskadda yashirilgan savolning javobi
        // shu yerdan olib tashlanadi, keyingi shartlar uni "javobsiz" deb ko'radi (`docs/18` §2.6).
        var workingAnswers = new Dictionary<string, AnswerSnapshot>(answersByQuestionCode, StringComparer.Ordinal);

        var visibleQuestionIds = new HashSet<Guid>();
        var visibleSectionIds = new HashSet<Guid>();
        var resolvedSectionVisibility = new Dictionary<Guid, bool>();

        bool EnsureSectionVisibility(Guid sectionId)
        {
            if (resolvedSectionVisibility.TryGetValue(sectionId, out var cached))
            {
                return cached;
            }

            var visible = sectionsById.TryGetValue(sectionId, out var section) && VisibilityEvaluator.Evaluate(section.Visibility, workingAnswers);
            resolvedSectionVisibility[sectionId] = visible;
            if (visible)
            {
                visibleSectionIds.Add(sectionId);
            }

            return visible;
        }

        foreach (var question in orderedQuestions)
        {
            var sectionVisible = question.SectionId is null || EnsureSectionVisibility(question.SectionId.Value);
            var visible = question.IsActive && sectionVisible && VisibilityEvaluator.Evaluate(question.Visibility, workingAnswers);

            if (visible)
            {
                visibleQuestionIds.Add(question.Id);
            }
            else
            {
                // Kaskad: yashirilgan savolning eski javobi keyingi shartlarga ta'sir qilmasin.
                workingAnswers.Remove(question.Code);
            }
        }

        // Hech qanday savolga tegishli bo'lmagan (yoki hali duch kelinmagan) bo'limlar ham
        // baholanadi — natijaviy `workingAnswers` bilan (`docs/18` §2.6: `VisibleSectionIds`
        // BARCHA bo'limlar bo'yicha to'liq bo'lishi kerak).
        foreach (var section in orderedSections)
        {
            EnsureSectionVisibility(section.Id);
        }

        return new VisibilityMap(visibleQuestionIds, visibleSectionIds);
    }
}

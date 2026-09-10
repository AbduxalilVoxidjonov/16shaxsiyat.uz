using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Import;

/// <summary>
/// `Custom` → `TestDefinition.AddQuestion` orqali TO'LIQ qo'shiladi (dublikat `Code` — ichki
/// tekshiruv `QUESTION_CODE_DUPLICATE` yoki DB unique cheklovi `ux_questions_code` —
/// ikkalasi ham `409`ga tushadi). Tizim testida FAQAT `Code` mos kelgan savolning matni
/// yangilanadi, `Scale`/`Direction`/`Weight` E'TIBORSIZ (BR-8) — mos kelmagan qatorlar
/// jimgina o'tkazib yuboriladi ("faqat matn yangilash" — yangi savol qo'shilmaydi).
/// </summary>
internal sealed class ImportTestQuestionsCommandHandler : IRequestHandler<ImportTestQuestionsCommand, Result<CatalogTestDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public ImportTestQuestionsCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogTestDetailDto>> Handle(ImportTestQuestionsCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.TestDefinitionId, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var importedCount = 0;

        if (test.IsSystem)
        {
            var questionsByCode = test.Questions.ToDictionary(q => q.Code, StringComparer.Ordinal);

            foreach (var item in request.Questions)
            {
                if (questionsByCode.TryGetValue(item.Code, out var existing))
                {
                    existing.UpdateText(item.TextUz, existing.TextRu, existing.TextEn);
                    importedCount++;
                }
            }
        }
        else
        {
            foreach (var item in request.Questions)
            {
                var questionType = Enum.Parse<QuestionType>(item.Type, ignoreCase: true);

                if (item.InputPattern is { Length: > 0 } pattern && !CachedInputPatternMatcher.IsValidPattern(pattern))
                {
                    return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.InputPatternInvalid, $"'{item.Code}' savolining InputPattern shabloni kompilyatsiya qilinmadi."));
                }

                Guid? sectionId = null;
                if (item.SectionCode is { Length: > 0 } sectionCode)
                {
                    var section = test.Sections.FirstOrDefault(s => s.Code == sectionCode);
                    if (section is null)
                    {
                        return Result.Failure<CatalogTestDetailDto>(new Error(ProblemCodes.NotFound, $"'{sectionCode}' kodli bo'lim topilmadi — savollardan OLDIN bo'limlar import qilinishi shart."));
                    }

                    sectionId = section.Id;
                }

                var question = Question.Create(
                    Guid.NewGuid(),
                    test.Id,
                    item.Code,
                    item.Order,
                    item.TextUz,
                    questionType,
                    item.Scale,
                    item.Direction,
                    item.Weight,
                    isRequired: item.IsRequired ?? true,
                    isSystem: false,
                    sectionId: sectionId,
                    visibilityRule: item.Visibility,
                    placeholder: item.Placeholder,
                    inputPattern: item.InputPattern,
                    maxLength: item.MaxLength,
                    minSelections: item.MinSelections,
                    maxSelections: item.MaxSelections);

                test.AddQuestion(question, now);
                // ⚠️ QA topilmasi — `CreateTestQuestionCommandHandler` izohiga qarang: `test`
                // so'rov orqali tracked, aniq `Add()` bo'lmasa yangi `Question` `Modified`
                // deb noto'g'ri xulosa chiqarilib "0 qator ta'sirlandi" bilan yiqiladi.
                _context.Add(question);

                foreach (var optionInput in item.Options ?? [])
                {
                    var option = AnswerOption.Create(Guid.NewGuid(), question.Id, optionInput.TextUz, optionInput.Value, optionInput.DisplayOrder);
                    question.AddOption(option);
                    _context.Add(option);
                }

                importedCount++;
            }
        }

        _context.Add(AuditLog.Create(
            AuditActions.CatalogQuestionAdded,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, ImportedCount = importedCount, test.IsSystem }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        var usedInProgramCount = await _executor.CountAsync(
            _context.AsNoTracking(_context.ProgramTests).Where(pt => pt.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToDetailDto(test, test.QuestionCount, test.Scales.Count, usedInProgramCount));
    }
}

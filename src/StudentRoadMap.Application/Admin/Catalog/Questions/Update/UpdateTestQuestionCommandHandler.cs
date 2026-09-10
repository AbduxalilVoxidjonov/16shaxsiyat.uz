using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Update;

internal sealed class UpdateTestQuestionCommandHandler : IRequestHandler<UpdateTestQuestionCommand, Result<CatalogQuestionItemDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public UpdateTestQuestionCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogQuestionItemDto>> Handle(UpdateTestQuestionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var question = await _executor.FirstOrDefaultAsync(
            _context.Questions.Where(q => q.Id == request.QuestionId),
            cancellationToken).ConfigureAwait(false);

        if (question is null)
        {
            return Result.Failure<CatalogQuestionItemDto>(new Error(ProblemCodes.NotFound, "Savol topilmadi."));
        }

        question.UpdateText(request.TextUz, request.TextRu, request.TextEn);

        if (request.IsActive)
        {
            question.Activate();
        }
        else
        {
            question.Deactivate();
        }

        if (request.IsRequired.HasValue)
        {
            question.UpdateRequired(request.IsRequired.Value);
        }

        if (request.Order.HasValue)
        {
            question.UpdateOrder(request.Order.Value);
        }

        // `Question.UpdateScale` `IsSystem` bo'lsa `SYSTEM_TEST_LOCKED` (BR-8) DomainException
        // otadi — global middleware ushlaydi (aniq TAHRIRLASH URINILGANDA, xohlagan
        // system-only maydonlarni (text/isActive) tinch qoldirganda emas).
        if (request.Scale is not null || request.Direction.HasValue || request.Weight.HasValue)
        {
            question.UpdateScale(
                request.Scale ?? question.Scale,
                request.Direction ?? question.ScaleDirection,
                request.Weight ?? question.Weight);
        }

        var testDefinition = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.TestDefinitions).Where(t => t.Id == question.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        // `docs/18` §5 — quyidagi maydonlar `IsSystem = false` savolda HAR DOIM to'liq
        // qo'llaniladi (`UpdateTestQuestionCommand` izohiga qarang); tizim savolida BUTUNLAY
        // e'tiborsiz qoldiriladi (BR-8: "avvalgidek faqat textUz/textRu/isActive"), aks holda
        // `AssignSection`/`UpdateVisibility` va h.k. `IsSystem` tekshiruvi bilan har chaqiriqda
        // `409 SYSTEM_TEST_LOCKED` otardi (qiymat o'zgarmasa ham).
        if (!question.IsSystem)
        {
            if (request.InputPattern is { Length: > 0 } pattern && !CachedInputPatternMatcher.IsValidPattern(pattern))
            {
                return Result.Failure<CatalogQuestionItemDto>(new Error(ProblemCodes.InputPatternInvalid, "InputPattern shabloni kompilyatsiya qilinmadi."));
            }

            if (request.Visibility is not null && testDefinition is { ScoringMode: TestScoringMode.Scored })
            {
                // `Question.UpdateVisibility` (Domain) B-2ni TEKSHIRMAYDI (faqat `AddQuestion`
                // vaqtida) — bu yerda Application qatlamida qo'lda qo'riqlanadi (`docs/18` B-2).
                return Result.Failure<CatalogQuestionItemDto>(new Error(ProblemCodes.BranchingNotAllowedInScored, "Ko'rsatish sharti faqat 'Survey' rejimidagi anketalarda ishlatiladi."));
            }

            Guid? sectionId = null;
            if (request.SectionCode is { Length: > 0 } sectionCode)
            {
                var section = await _executor.FirstOrDefaultAsync(
                    _context.AsNoTracking(_context.QuestionSections).Where(s => s.TestDefinitionId == question.TestDefinitionId && s.Code == sectionCode),
                    cancellationToken).ConfigureAwait(false);

                if (section is null)
                {
                    return Result.Failure<CatalogQuestionItemDto>(new Error(ProblemCodes.NotFound, $"'{sectionCode}' kodli bo'lim topilmadi."));
                }

                sectionId = section.Id;
            }

            question.AssignSection(sectionId);
            question.UpdatePlaceholder(request.Placeholder);
            question.UpdateInputPattern(request.InputPattern);
            question.UpdateMaxLength(request.MaxLength);
            question.UpdateSelectionLimits(request.MinSelections, request.MaxSelections);
            question.UpdateVisibility(request.Visibility);

            if (request.Options is not null)
            {
                // Variantlarni TO'LIQ almashtirish (`docs/18` §5) — avval mavjudlarini tracked
                // holda yuklab (EF fixup, `LoadTrackedAsync` izohidagi naqsh), keyin o'chirib
                // qayta qo'shamiz. `RemoveOption` faqat in-memory ro'yxatdan chiqaradi — EF
                // orfan (zarur bog'lanish) qatorlarni avtomatik `Deleted` deb belgilaydi.
                _ = await _executor.ToListAsync(
                    _context.AnswerOptions.Where(o => o.QuestionId == question.Id),
                    cancellationToken).ConfigureAwait(false);

                foreach (var existing in question.Options.ToList())
                {
                    question.RemoveOption(existing.Id);
                }

                foreach (var optionInput in request.Options)
                {
                    var newOption = AnswerOption.Create(Guid.NewGuid(), question.Id, optionInput.TextUz, optionInput.Value, optionInput.DisplayOrder);
                    question.AddOption(newOption);
                    _context.Add(newOption);
                }
            }
        }

        _context.Add(AuditLog.Create(
            AuditActions.CatalogQuestionUpdated,
            now,
            request.AdminUserId,
            entityType: "Question",
            entityId: question.Id,
            afterJson: AuditSnapshot.Serialize(new { question.Id, question.TextUz, question.IsActive }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (testDefinition is not null)
        {
            _catalogCache.InvalidateTestDefinition(testDefinition.Id, testDefinition.Code);
        }

        // `testDefinition` `AsNoTracking` so'rov bilan olingan — `Scales` kolleksiyasi BO'SH,
        // shu sabab shkalalar alohida yuklanadi. Anketa topilmasa (nazariy holat: savol bor,
        // testi yo'q) nom berilmaydi — noto'g'ri nomdan ko'ra `null` yaxshi.
        var scaleNames = testDefinition is null
            ? CatalogScaleNameResolver.None
            : await CatalogMapping.LoadScaleNamesAsync(_context, _executor, testDefinition, cancellationToken).ConfigureAwait(false);

        return Result.Success(CatalogMapping.ToQuestionDto(question, scaleNames));
    }
}

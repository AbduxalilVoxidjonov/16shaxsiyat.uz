using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Reorder;

internal sealed class ReorderTestQuestionsCommandHandler : IRequestHandler<ReorderTestQuestionsCommand, Result<IReadOnlyList<CatalogQuestionItemDto>>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public ReorderTestQuestionsCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<IReadOnlyList<CatalogQuestionItemDto>>> Handle(ReorderTestQuestionsCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.TestDefinitionId, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<IReadOnlyList<CatalogQuestionItemDto>>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var questionsById = test.Questions.ToDictionary(q => q.Id);

        foreach (var item in request.Items)
        {
            if (!questionsById.TryGetValue(item.Id, out var question))
            {
                return Result.Failure<IReadOnlyList<CatalogQuestionItemDto>>(new Error(
                    ProblemCodes.ValidationError,
                    $"'{item.Id}' savoli bu anketaga tegishli emas."));
            }

            question.UpdateOrder(item.DisplayOrder);
        }

        _context.Add(AuditLog.Create(
            AuditActions.CatalogQuestionUpdated,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, ReorderedCount = request.Items.Count }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        // `LoadTrackedAsync` shkalalarni ham yuklaydi — resolver agregatning o'zidan quriladi.
        var scaleNames = CatalogScaleNameResolver.ForTest(test);

        // P52: `HasAnswers` bu yerda ham (natija ro'yxat) batch so'rov bilan (N+1 emas).
        var questionIdsWithAnswers = await CatalogMapping.LoadQuestionIdsWithAnswersAsync(
            _context, _executor, test.Questions.Select(q => q.Id).ToList(), cancellationToken).ConfigureAwait(false);

        var items = test.Questions
            .OrderBy(q => q.DisplayOrder)
            .Select(q => CatalogMapping.ToQuestionDto(q, scaleNames, hasAnswers: questionIdsWithAnswers.Contains(q.Id)))
            .ToList();

        return Result.Success<IReadOnlyList<CatalogQuestionItemDto>>(items);
    }
}

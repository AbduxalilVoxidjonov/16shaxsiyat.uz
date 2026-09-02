using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
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

        return Result.Success(CatalogMapping.ToQuestionDto(question));
    }
}

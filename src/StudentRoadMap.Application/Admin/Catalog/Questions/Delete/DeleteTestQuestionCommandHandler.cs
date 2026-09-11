using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Delete;

/// <summary>`TestDefinition.RemoveQuestion` (Domain) `SYSTEM_TEST_LOCKED`ni bevosita ko'taradi (BR-8). Nashr qilingan testdan olib tashlansa `Version` oshadi (BR-9) — kesh bekor qilinadi.</summary>
internal sealed class DeleteTestQuestionCommandHandler : IRequestHandler<DeleteTestQuestionCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public DeleteTestQuestionCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result> Handle(DeleteTestQuestionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var question = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Questions).Where(q => q.Id == request.QuestionId),
            cancellationToken).ConfigureAwait(false);

        if (question is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Savol topilmadi."));
        }

        // QA topilmasi (2026-09-11): egasi javobi bor savolni o'chirmoqchi bo'lgan va
        // `fk_answers_questions_question_id` (`23503`) jimgina `500 INTERNAL_ERROR` bo'lib
        // chiqqan. O'chirishdan OLDIN `EXISTS` (`AnyAsync` → SQL `EXISTS`, `COUNT` EMAS) bilan
        // tekshiriladi — aniq va harakatga yo'naltiruvchi `409 QUESTION_IN_USE`.
        var hasAnswers = await _executor.AnyAsync(
            _context.AsNoTracking(_context.Answers).Where(a => a.QuestionId == request.QuestionId),
            cancellationToken).ConfigureAwait(false);

        if (hasAnswers)
        {
            return Result.Failure(new Error(
                ProblemCodes.QuestionInUse,
                "Bu savolga allaqachon javob berilgan, shuning uchun uni o'chirib bo'lmaydi. " +
                "Uni yangi sessiyalarda ko'rsatmaslik uchun \"Faol emas\" holatiga o'tkazing — " +
                "eski javoblar va hisobotlar saqlanib qoladi."));
        }

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, question.TestDefinitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Savolning anketasi topilmadi (ma'lumot izchilligi buzilgan).");

        // `TestDefinition.RemoveQuestion` (Domain) `SYSTEM_TEST_LOCKED` (BR-8) va
        // `QUESTION_REFERENCED_BY_VISIBILITY` (`docs/18` B-5) — ikkalasi ham DomainException,
        // global middleware ushlaydi (`SCALE_IN_USE`/`SECTION_IN_USE` bilan bir xil naqsh).
        test.RemoveQuestion(request.QuestionId, now);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogQuestionRemoved,
            now,
            request.AdminUserId,
            entityType: "Question",
            entityId: request.QuestionId,
            beforeJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, question.Code }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        return Result.Success();
    }
}

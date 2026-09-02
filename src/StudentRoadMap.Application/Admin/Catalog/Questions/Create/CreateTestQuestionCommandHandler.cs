using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Questions.Create;

/// <summary>
/// `TestDefinition.AddQuestion` (Domain) `SYSTEM_TEST_LOCKED`/`QUESTION_CODE_DUPLICATE`
/// DomainException'ini bevosita ko'taradi. Nashr qilingan testga qo'shilsa `Version` oshadi
/// (BR-9) — ommaviy kesh har doim bekor qilinadi ("ENG MUHIM" #2).
/// </summary>
internal sealed class CreateTestQuestionCommandHandler : IRequestHandler<CreateTestQuestionCommand, Result<CatalogQuestionItemDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;
    private readonly PublicCatalogCache _catalogCache;

    public CreateTestQuestionCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher, PublicCatalogCache catalogCache)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
        _catalogCache = catalogCache;
    }

    public async Task<Result<CatalogQuestionItemDto>> Handle(CreateTestQuestionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await CatalogMapping.LoadTrackedAsync(_context, _executor, request.TestDefinitionId, cancellationToken).ConfigureAwait(false);
        if (test is null)
        {
            return Result.Failure<CatalogQuestionItemDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        var questionType = Enum.Parse<QuestionType>(request.Type, ignoreCase: true);

        var question = Question.Create(
            Guid.NewGuid(),
            test.Id,
            request.Code,
            request.Order,
            request.TextUz,
            questionType,
            request.Scale,
            request.Direction,
            request.Weight,
            isRequired: request.IsRequired ?? true,
            isSystem: false,
            textRu: request.TextRu,
            textEn: request.TextEn);

        test.AddQuestion(question, now);

        // ⚠️ QA topilmasi (`AddProgramTestCommandHandler`dagi izohga qarang, bir xil sabab):
        // `test` SO'ROV orqali tracked qilingan (`_context.Add(test)` EMAS) — domen metodi
        // ichida yaratilgan `Question` EF'ning avtomatik graf kashfiyoti orqali topilsa, `Id`
        // OLDINDAN o'rnatilgani sabab `Modified` (mavjud qator) deb noto'g'ri xulosa chiqadi va
        // "0 qator ta'sirlandi" (`DbUpdateConcurrencyException`) bilan yiqiladi. Aniq `Add()`
        // holatni to'g'ri `Added`ga majburlaydi.
        _context.Add(question);

        _context.Add(AuditLog.Create(
            AuditActions.CatalogQuestionAdded,
            now,
            request.AdminUserId,
            entityType: "Question",
            entityId: question.Id,
            afterJson: AuditSnapshot.Serialize(new { TestDefinitionId = test.Id, question.Code, question.Scale }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _catalogCache.InvalidateTestDefinition(test.Id, test.Code);

        return Result.Success(CatalogMapping.ToQuestionDto(question));
    }
}

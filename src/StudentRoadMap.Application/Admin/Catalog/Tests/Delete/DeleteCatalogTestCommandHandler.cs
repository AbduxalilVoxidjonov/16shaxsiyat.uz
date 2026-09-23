using MediatR;
using StudentRoadMap.Application.Admin.Catalog.Tests.Assignment;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Catalog.Tests.Delete;

/// <summary>
/// Qattiq (hard) o'chirish — `TestDefinition`da soft-delete maydoni yo'q (`docs/04` §5 faqat
/// `School`/`Student`/`Assessment`ni sanaydi). `IsSystem` → `409 SYSTEM_TEST_LOCKED` (BR-8: "bu
/// testlar o'chirilmaydi"). Dastur (`ProgramTest`) yoki sessiyada (`AssessmentTest`) ishlatilgan
/// bo'lsa → `409 TEST_IN_USE` (`docs/07` §3.4, BR-11 ruhida).
/// </summary>
internal sealed class DeleteCatalogTestCommandHandler : IRequestHandler<DeleteCatalogTestCommand, Result>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public DeleteCatalogTestCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result> Handle(DeleteCatalogTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var test = await _executor.FirstOrDefaultAsync(
            _context.TestDefinitions.Where(t => t.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (test is null)
        {
            return Result.Failure(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        if (test.IsSystem)
        {
            return Result.Failure(new Error(ProblemCodes.SystemTestLocked, "Tizim metodikasi o'chirilmaydi (BR-8)."));
        }

        // 2026-09-23 (`docs/18` §9.7): testning O'Z test dasturi "boshqa dasturda ishlatilgan"
        // hisoblanmaydi — u test bilan birga o'chiriladi (maktab biriktirmalari ham, kaskad).
        // Sessiyasi bor bo'lsa baribir `usedInSession` to'xtatadi (`assessment_tests`).
        var ownProgram = await TestPrograms.FindAsync(_context, _executor, test.Id, cancellationToken).ConfigureAwait(false);
        var ownProgramId = ownProgram?.Id;

        var usedInProgram = await _executor.AnyAsync(
            _context.ProgramTests.Where(pt => pt.TestDefinitionId == test.Id && pt.ProgramId != ownProgramId),
            cancellationToken).ConfigureAwait(false);

        var usedInSession = await _executor.AnyAsync(
            _context.AssessmentTests.Where(at => at.TestDefinitionId == test.Id),
            cancellationToken).ConfigureAwait(false);

        if (usedInProgram || usedInSession)
        {
            return Result.Failure(new Error(ProblemCodes.TestInUse, "Bu anketa dastur yoki sessiyada ishlatilgan — o'chirish o'rniga arxivlang."));
        }

        _context.Add(AuditLog.Create(
            AuditActions.CatalogTestDeleted,
            now,
            request.AdminUserId,
            entityType: "TestDefinition",
            entityId: test.Id,
            beforeJson: AuditSnapshot.Serialize(new { test.Id, test.Code, test.NameUz }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        if (ownProgram is not null)
        {
            var ownProgramHasSessions = await _executor.AnyAsync(
                _context.Assessments.Where(a => a.ProgramId == ownProgram.Id),
                cancellationToken).ConfigureAwait(false);

            if (ownProgramHasSessions)
            {
                return Result.Failure(new Error(ProblemCodes.TestInUse, "Bu anketa sessiyada ishlatilgan — o'chirish o'rniga arxivlang."));
            }

            _context.Remove(ownProgram);
        }

        _context.Remove(test);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

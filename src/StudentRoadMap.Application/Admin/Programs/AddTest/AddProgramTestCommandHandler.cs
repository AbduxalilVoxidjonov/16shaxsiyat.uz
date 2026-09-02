using MediatR;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Application.Admin.Programs.AddTest;

internal sealed class AddProgramTestCommandHandler : IRequestHandler<AddProgramTestCommand, Result<AdminProgramDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly IIpHasher _ipHasher;

    public AddProgramTestCommandHandler(IAppDbContext context, IAsyncQueryExecutor executor, IDateTime dateTime, IIpHasher ipHasher)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _ipHasher = ipHasher;
    }

    public async Task<Result<AdminProgramDetailDto>> Handle(AddProgramTestCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var program = await _executor.FirstOrDefaultAsync(
            _context.AssessmentPrograms.Where(p => p.Id == request.ProgramId),
            cancellationToken).ConfigureAwait(false);

        if (program is null)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        var testDefinitionExists = await _executor.AnyAsync(
            _context.TestDefinitions.Where(t => t.Id == request.TestDefinitionId),
            cancellationToken).ConfigureAwait(false);

        if (!testDefinitionExists)
        {
            return Result.Failure<AdminProgramDetailDto>(new Error(ProblemCodes.NotFound, "Anketa topilmadi."));
        }

        program.AddTest(request.TestDefinitionId, request.DisplayOrder, now);

        // ⚠️ QA topilmasi (SQLite VA Postgres'da bir xil): `program` avval SO'ROV orqali
        // (Add() EMAS) tracked qilingan — domen metodi ichida yaratilgan yangi `ProgramTest`
        // EF Core'ning avtomatik graf kashfiyoti (`DetectChanges`) orqali topiladi, lekin
        // `ProgramTest.Id` OLDINDAN (client tomonida) o'rnatilgani sabab EF uni "allaqachon
        // bazada mavjud" deb noto'g'ri xulosa chiqaradi (`Modified`, `Added` emas) — natijada
        // haqiqatda mavjud bo'lmagan qatorga `UPDATE ... WHERE id = @p` yuboriladi va
        // "0 qator ta'sirlandi" (`DbUpdateConcurrencyException`) bilan yiqiladi. Yangi
        // yaratilgan `ProgramTest`ni ANIQ `Add()` qilish holatni to'g'ri `Added`ga majburlaydi
        // (`Assessment`/`AssessmentTest` bunday muammoga duch kelmaydi, chunki ular DOIM yangi
        // `Assessment` ROOT'i bilan birga bitta `_context.Add(assessment)` orqali qo'shiladi —
        // bu yerda esa `program` mavjud, faqat TARKIBIGA yangi element qo'shilmoqda).
        var newProgramTest = program.Tests.Single(t => t.TestDefinitionId == request.TestDefinitionId);
        _context.Add(newProgramTest);

        _context.Add(AuditLog.Create(
            AuditActions.ProgramTestAdded,
            now,
            request.AdminUserId,
            entityType: "AssessmentProgram",
            entityId: program.Id,
            afterJson: AuditSnapshot.Serialize(new { program.Id, request.TestDefinitionId, request.DisplayOrder }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await ProgramMapping.BuildDetailDtoAsync(_context, _executor, program, cancellationToken).ConfigureAwait(false);

        return Result.Success(dto);
    }
}

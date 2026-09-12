using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartPublicSession;

/// <summary>
/// Ommaviy (maktabsiz) sessiya ochish — `docs/07` 5.4-bo'lim.
///
/// Oqim (maktab oqimidagi BR-1/BR-5 qoidalari AYNAN saqlanadi — `ProgramSessionPolicy`):
/// 1. Foydalanuvchi mavjudmi (o'chirilgan akkaunt global filtr bilan yashiringan);
/// 2. Ommaviy makon (`SchoolKind.PublicSpace`) topiladi — yo'q bo'lsa `PUBLIC_SPACE_NOT_CONFIGURED`
///    (seed bajarilmagan), faol bo'lmasa `SCHOOL_INACTIVE`;
/// 3. Shu foydalanuvchining ommaviy makondagi `Student` yozuvi qidiriladi, profil
///    majburiyligi tekshiriladi (pastga qarang);
/// 4. Dastur tanlanadi (`ProgramAvailability` — maktab oqimi bilan BIR XIL mezon). Sessiya
///    holatidan OLDIN: BR-1/BR-5 **`(o'quvchi, dastur)` juftligi** bo'yicha (egasining qarori,
///    2026-09-07) — makonda bir nechta dastur bo'lsa foydalanuvchi ularning har birini
///    topshira oladi;
/// 5. Mavjud `Student` bo'lsa, TANLANGAN DASTUR bo'yicha: tugallanmagan muddati o'tmagan
///    sessiya → `resumed: true`; muddati o'tgan → `Abandoned` (BR-5); 90 kun ichida
///    yakunlangan → `409 DUPLICATE_ASSESSMENT`. Boshqa dasturdagi sessiyalar ta'sir qilmaydi;
/// 6. Kunlik hisoblagich atomik oshiriladi (faqat YANGI sessiya oldidan — maktab oqimidagi
///    PM qarori bilan bir xil);
/// 7. `Assessment` yaratiladi va dastur testlari `AssessmentTestAttacher` bilan biriktiriladi.
///
/// **O'quvchi qidirish mezoni maktabnikidan farq qiladi va bu ataylab:** maktabda
/// `(SchoolId, NormalizedName, BirthDate)` (ro'yxatdan o'tish yo'q, shaxs shu uchlik bilan
/// aniqlanadi), bu yerda esa `(SchoolId, PublicUserId)` — akkaunt allaqachon shaxsni
/// aniqlaydi va F.I.Sh. o'zgarishi (masalan familiya almashishi) yangi "o'quvchi" yaratib
/// tarixni ikkiga bo'lib yubormasligi kerak.
///
/// **Anketa qayta so'ralmaydi (2026-09-07).** Shaxsiy maydonlar buyruqda ixtiyoriy; qaysi
/// biri MAJBURIY — `Student` topilganidan keyin `PublicStudentProfile.RequireFields` hal qiladi
/// (validator DB ko'rmaydi — `StartPublicSessionCommandValidator` izohi). Xato shakli
/// `ValidationBehavior` bilan AYNAN bir xil: `400 VALIDATION_ERROR` + `errors{maydon:[xabar]}`,
/// frontend ikkalasini bir xil kodda maydonlarga bog'laydi. Mavjud `Student` da kelgan
/// maydonlar tahrir sifatida qo'llanadi (`Student.UpdateProfile`), rozilik faqat
/// `ConsentVersion` eskirgan bo'lsa qayta so'raladi — `ConsentGivenAt` (rozilik isboti sanasi)
/// har sessiyada qayta yozilmaydi.
///
/// **Profil mantiqi umumiy (2026-09-07).** Majburiylik, tahrir va `Student` yaratish
/// `PublicStudentProfile` da — `UpdateStudentProfileCommandHandler` (`PUT /api/me/profile`,
/// sessiyasiz saqlash) bilan BIR XIL qoidalar. Bu handlerda faqat sessiya mantiqi qoldi.
/// </summary>
internal sealed class StartPublicSessionCommandHandler : IRequestHandler<StartPublicSessionCommand, Result<StartSessionResult>>
{
    private const int SessionTokenByteLength = 32;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public StartPublicSessionCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        ITokenGenerator tokenGenerator,
        IIpHasher ipHasher,
        IAppSettings appSettings)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _tokenGenerator = tokenGenerator;
        _ipHasher = ipHasher;
        _appSettings = appSettings;
    }

    public async Task<Result<StartSessionResult>> Handle(StartPublicSessionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var userExists = await _executor.AnyAsync(
            _context.AsNoTracking(_context.PublicUsers).Where(u => u.Id == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (!userExists)
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.PublicUserDeleted, "Bu akkaunt mavjud emas."));
        }

        var space = await PublicStudentProfile.FindPublicSpaceAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<StartSessionResult>(PublicStudentProfile.PublicSpaceNotConfigured());
        }

        if (!space.IsActive)
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.SchoolInactive, "Ommaviy testlar hozircha faol emas."));
        }

        var existingStudent = await PublicStudentProfile
            .FindStudentAsync(_context, _executor, space.Id, request.PublicUserId, cancellationToken)
            .ConfigureAwait(false);

        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // P52 2-to'lqin (2026-09-12, `docs/18` §9.6.2): GLOBAL sozlama — dastur ustunligi
        // BU YERDA QO'LLANMAYDI (`RegistrationFormResolver` sinf izohiga qarang: profil
        // dastur tanlanishidan OLDIN so'raladi, "bir marta so'raladi" naqshi saqlanadi).
        var globalDefinition = await RegistrationFormResolver.GetGlobalDefinitionAsync(_context, _executor, cancellationToken).ConfigureAwait(false);

        var profileErrors = PublicStudentProfile.RequireFields(request, existingStudent, today, globalDefinition.CoreFields);
        var (customFieldErrors, validatedCustomFields) = PublicStudentProfile.ValidateCustomFields(globalDefinition.CustomFields, request, existingStudent);
        foreach (var (key, value) in customFieldErrors)
        {
            profileErrors[key] = value;
        }

        if (profileErrors.Count > 0)
        {
            return Result.Failure<StartSessionResult>(PublicStudentProfile.ValidationFailure(profileErrors));
        }

        // Dastur — sessiya holati tekshiruvidan OLDIN (sinf izohi, 4-qadam): BR-1/BR-5 shu
        // dastur bo'yicha ishlaydi. `400`/`404` bo'lsa hisoblagich oshirilmaydi, profil
        // tahriri ham saqlanmaydi (`409` dagi kabi — muvaffaqiyatsiz so'rov hech narsa yozmaydi).
        var programResult = await ResolveProgramAsync(space.Id, request.ProgramCode, cancellationToken).ConfigureAwait(false);
        if (programResult.IsFailure)
        {
            return Result.Failure<StartSessionResult>(programResult.Error);
        }

        var program = programResult.Value;

        Student student;
        var isNewStudent = existingStudent is null;

        if (existingStudent is not null)
        {
            // Tahrir va rozilik sessiya holatidan OLDIN qo'llanadi: foydalanuvchi "O'zgartirish"
            // bosgan bo'lsa, tugallanmagan sessiya davom etayotganida ham yangi F.I.Sh./telefon
            // saqlanishi kerak (davom ettirish shoxida `SaveChangesAsync` shu sabab bor).
            var applyResult = PublicStudentProfile.ApplyChanges(request, existingStudent, now, validatedCustomFields);
            if (applyResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(applyResult.Error);
            }

            // Faqat TANLANGAN dasturdagi tugallanmagan sessiya (`ProgramSessionPolicy`).
            var unfinished = await ProgramSessionPolicy
                .FindUnfinishedAsync(_context, _executor, existingStudent.Id, program.Id, cancellationToken)
                .ConfigureAwait(false);

            if (unfinished is not null)
            {
                if (unfinished.ExpiresAt > now)
                {
                    var resumedTests = await BuildTestSummariesAsync(unfinished.Id, cancellationToken).ConfigureAwait(false);

                    // Profil tahriri (bo'lsa) davom ettirishda ham saqlanadi — `Student` kuzatuvda.
                    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    return Result.Success(new StartSessionResult(
                        unfinished.SessionToken,
                        unfinished.Id,
                        unfinished.Status.ToString(),
                        unfinished.ExpiresAt,
                        Resumed: true,
                        resumedTests));
                }

                // BR-5: muddati o'tgan sessiya davom ettirilmaydi.
                unfinished.MarkAbandoned(now);
            }

            // BR-1 — shu DASTUR bo'yicha 90 kunlik oyna.
            var hasRecentCompleted = await ProgramSessionPolicy
                .HasCompletedWithinWindowAsync(_context, _executor, existingStudent.Id, program.Id, now, cancellationToken)
                .ConfigureAwait(false);

            if (hasRecentCompleted)
            {
                return Result.Failure<StartSessionResult>(new Error(
                    ProblemCodes.DuplicateAssessment,
                    "Siz so'nggi 90 kun ichida bu dasturni allaqachon yakunlagansiz."));
            }

            student = existingStudent;
        }
        else
        {
            // `RequireFields` yangi profil uchun to'liq to'plamni tekshirib bo'ldi.
            var createResult = PublicStudentProfile.CreateStudent(request, space.Id, request.PublicUserId, now, validatedCustomFields);
            if (createResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(createResult.Error);
            }

            student = createResult.Value;
        }

        // Kunlik limit — ommaviy makonda ancha katta (`School.DefaultPublicSpaceRegistrationLimit`),
        // lekin mexanizm bir xil: atomik oshirish, faqat YANGI sessiya oldidan.
        var dateUtc = DateOnly.FromDateTime(now.UtcDateTime);
        var registrationCount = await _context
            .IncrementRegistrationCounterAsync(space.Id, dateUtc, cancellationToken)
            .ConfigureAwait(false);

        if (registrationCount > space.DailyRegistrationLimit)
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.RateLimited, "Bugungi kunlik limit tugadi. Ertaga qayta urinib ko'ring."));
        }

        var sessionToken = _tokenGenerator.GenerateUrlSafeToken(SessionTokenByteLength);
        var expiresAt = now.AddDays(_appSettings.SessionLifetimeDays);

        var assessment = Assessment.Create(
            Guid.NewGuid(),
            student.Id,
            space.Id,
            sessionToken,
            string.IsNullOrWhiteSpace(request.LanguageCode) ? "uz" : request.LanguageCode,
            program.Id,
            startedAt: now,
            expiresAt: expiresAt,
            now: now,
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent);

        var tests = await AssessmentTestAttacher.AttachAsync(_context, _executor, assessment, program.Id, cancellationToken)
            .ConfigureAwait(false);

        if (isNewStudent)
        {
            _context.Add(student);
        }

        _context.Add(assessment);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new StartSessionResult(
            sessionToken,
            assessment.Id,
            assessment.Status.ToString(),
            expiresAt,
            Resumed: false,
            tests));
    }

    private async Task<IReadOnlyList<PublicTestSummaryDto>> BuildTestSummariesAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessmentId).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).ToList();
        var definitions = await _executor.ToListAsync(
            _context.TestDefinitions
                .Where(t => testDefinitionIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Code, t.NameUz, t.EstimatedMinutes }),
            cancellationToken).ConfigureAwait(false);

        var definitionLookup = definitions.ToDictionary(x => x.Id, x => x);

        return assessmentTests
            .Select(t =>
            {
                var definition = definitionLookup.GetValueOrDefault(t.TestDefinitionId);
                return new PublicTestSummaryDto(
                    definition?.Code ?? "?",
                    definition?.NameUz ?? definition?.Code ?? "?",
                    t.Status.ToString(),
                    t.AnsweredCount,
                    t.TotalCount,
                    t.DisplayOrder,
                    definition?.EstimatedMinutes ?? 0);
            })
            .ToList();
    }

    /// <summary>
    /// Dastur tanlash — maktab oqimi bilan BIR XIL mezon (`ProgramAvailability`), lekin
    /// "birorta dastur yo'q" holati ALOHIDA kod bilan qaytadi: `409 NO_PROGRAM_AVAILABLE`
    /// ("makon sozlanmagan, admin dastur biriktirishi kerak") — maktab oqimidagi
    /// `400 PROGRAM_REQUIRED` ("siz tanlashingiz kerak") mijozni chalg'itardi, chunki
    /// tanlaydigan narsa umuman yo'q. Maktab oqimining xatti-harakati o'zgarmadi.
    /// </summary>
    private async Task<Result<AssessmentProgram>> ResolveProgramAsync(Guid spaceId, string? programCode, CancellationToken cancellationToken)
    {
        var availablePrograms = await ProgramAvailability
            .GetAvailableProgramsAsync(_context, _executor, spaceId, cancellationToken)
            .ConfigureAwait(false);

        if (availablePrograms.Count == 0)
        {
            return Result.Failure<AssessmentProgram>(new Error(
                ProblemCodes.NoProgramAvailable,
                "Ommaviy makonga hech qanday dastur biriktirilmagan."));
        }

        if (string.IsNullOrWhiteSpace(programCode))
        {
            return availablePrograms.Count == 1
                ? Result.Success(availablePrograms[0])
                : Result.Failure<AssessmentProgram>(new Error(ProblemCodes.ProgramRequired, "Bir nechta dastur mavjud — dastur tanlanishi shart."));
        }

        var matched = availablePrograms.FirstOrDefault(p => p.Code == programCode);

        return matched is null
            ? Result.Failure<AssessmentProgram>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."))
            : Result.Success(matched);
    }
}

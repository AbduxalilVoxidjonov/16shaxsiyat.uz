using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Application.Public.StartSession;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartPublicSession;

/// <summary>
/// Ommaviy (maktabsiz) sessiya ochish — `docs/07` 5.4-bo'lim.
///
/// Oqim (maktab oqimidagi BR-1/BR-5 qoidalari AYNAN saqlanadi):
/// 1. Foydalanuvchi mavjudmi (o'chirilgan akkaunt global filtr bilan yashiringan);
/// 2. Ommaviy makon (`SchoolKind.PublicSpace`) topiladi — yo'q bo'lsa `PUBLIC_SPACE_NOT_CONFIGURED`
///    (seed bajarilmagan), faol bo'lmasa `SCHOOL_INACTIVE`;
/// 3. Shu foydalanuvchining ommaviy makondagi `Student` yozuvi qidiriladi:
///    tugallanmagan muddati o'tmagan sessiya → `resumed: true`; muddati o'tgan → `Abandoned`
///    (BR-5); 90 kun ichida yakunlangan → `409 DUPLICATE_ASSESSMENT`;
/// 4. Dastur tanlanadi (`ProgramAvailability` — maktab oqimi bilan BIR XIL mezon);
/// 5. Kunlik hisoblagich atomik oshiriladi (faqat YANGI sessiya oldidan — maktab oqimidagi
///    PM qarori bilan bir xil);
/// 6. `Assessment` yaratiladi va dastur testlari `AssessmentTestAttacher` bilan biriktiriladi.
///
/// **O'quvchi qidirish mezoni maktabnikidan farq qiladi va bu ataylab:** maktabda
/// `(SchoolId, NormalizedName, BirthDate)` (ro'yxatdan o'tish yo'q, shaxs shu uchlik bilan
/// aniqlanadi), bu yerda esa `(SchoolId, PublicUserId)` — akkaunt allaqachon shaxsni
/// aniqlaydi va F.I.Sh. o'zgarishi (masalan familiya almashishi) yangi "o'quvchi" yaratib
/// tarixni ikkiga bo'lib yubormasligi kerak.
///
/// **Anketa qayta so'ralmaydi (2026-09-07).** Shaxsiy maydonlar buyruqda ixtiyoriy; qaysi
/// biri MAJBURIY — `Student` topilganidan keyin <see cref="RequireProfileFields"/> hal qiladi
/// (validator DB ko'rmaydi — `StartPublicSessionCommandValidator` izohi). Xato shakli
/// `ValidationBehavior` bilan AYNAN bir xil: `400 VALIDATION_ERROR` + `errors{maydon:[xabar]}`,
/// frontend ikkalasini bir xil kodda maydonlarga bog'laydi. Mavjud `Student` da kelgan
/// maydonlar tahrir sifatida qo'llanadi (`Student.UpdateProfile`), rozilik faqat
/// `ConsentVersion` eskirgan bo'lsa qayta so'raladi — `ConsentGivenAt` (rozilik isboti sanasi)
/// har sessiyada qayta yozilmaydi.
/// </summary>
internal sealed class StartPublicSessionCommandHandler : IRequestHandler<StartPublicSessionCommand, Result<StartSessionResult>>
{
    private const int SessionTokenByteLength = 32;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromDays(90);

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

        // Ommaviy makon bazada AYNAN BITTA (`ux_schools_public_space` qisman unikal indeks,
        // `SchoolKind` izohi) — `Id` konstantasi bo'yicha emas, TUR bo'yicha qidiriladi:
        // `DbSeeder.PublicSpaceSchoolId` `Infrastructure` qatlamida va `Application` unga
        // bog'lana olmaydi (`docs/06` 3-bo'lim).
        var space = await _executor.FirstOrDefaultAsync(
            _context.Schools.Where(s => s.Kind == SchoolKind.PublicSpace),
            cancellationToken).ConfigureAwait(false);

        if (space is null)
        {
            return Result.Failure<StartSessionResult>(new Error(
                ProblemCodes.PublicSpaceNotConfigured,
                "Ommaviy makon sozlanmagan. Administratorga murojaat qiling."));
        }

        if (!space.IsActive)
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.SchoolInactive, "Ommaviy testlar hozircha faol emas."));
        }

        var existingStudent = await _executor.FirstOrDefaultAsync(
            _context.Students.Where(s => s.SchoolId == space.Id && s.PublicUserId == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var profileErrors = RequireProfileFields(request, existingStudent, today);
        if (profileErrors.Count > 0)
        {
            return Result.Failure<StartSessionResult>(ValidationFailure(profileErrors));
        }

        Student student;
        var isNewStudent = existingStudent is null;

        if (existingStudent is not null)
        {
            // Tahrir va rozilik sessiya holatidan OLDIN qo'llanadi: foydalanuvchi "O'zgartirish"
            // bosgan bo'lsa, tugallanmagan sessiya davom etayotganida ham yangi F.I.Sh./telefon
            // saqlanishi kerak (davom ettirish shoxida `SaveChangesAsync` shu sabab bor).
            var applyResult = ApplyProfileChanges(request, existingStudent, now);
            if (applyResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(applyResult.Error);
            }

            // TODO(P30): mijoz tomonida saralash — SQLite `DateTimeOffset` bo'yicha ORDER BY'ni
            // tarjima qila olmaydi (`StartSessionCommandHandler` dagi bir xil izoh/texnik qarz).
            var unfinishedCandidates = await _executor.ToListAsync(
                _context.Assessments
                    .Where(a => a.StudentId == existingStudent.Id && (a.Status == AssessmentStatus.Draft || a.Status == AssessmentStatus.InProgress)),
                cancellationToken).ConfigureAwait(false);

            var unfinished = unfinishedCandidates.OrderByDescending(a => a.StartedAt).FirstOrDefault();

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

            var completedAssessments = await _executor.ToListAsync(
                _context.Assessments.Where(a => a.StudentId == existingStudent.Id && a.CompletedAt != null),
                cancellationToken).ConfigureAwait(false);

            if (completedAssessments.Any(a => a.CompletedAt!.Value >= now - DuplicateWindow))
            {
                return Result.Failure<StartSessionResult>(new Error(
                    ProblemCodes.DuplicateAssessment,
                    "Siz so'nggi 90 kun ichida testni allaqachon yakunlagansiz."));
            }

            student = existingStudent;
        }
        else
        {
            // `RequireProfileFields` yangi profil uchun to'liq to'plamni tekshirib bo'ldi —
            // bu yerda `!`/`.Value` xavfsiz.
            var phoneResult = PhoneNumber.Create(request.Phone!);
            if (phoneResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(phoneResult.Error);
            }

            student = Student.Create(
                Guid.NewGuid(),
                space.Id,
                request.FullName!,
                request.BirthDate!.Value,
                request.Gender!.Value,
                request.Grade ?? Student.NoGrade,
                phoneResult.Value,
                consentGivenAt: now,
                now: now,
                email: string.IsNullOrWhiteSpace(request.Email) ? null : request.Email,
                publicUserId: request.PublicUserId,
                consentVersion: PublicConsent.CurrentVersion,
                parentalConsent: request.ParentalConsent ?? false);
        }

        var programResult = await ResolveProgramAsync(space.Id, request.ProgramCode, cancellationToken).ConfigureAwait(false);
        if (programResult.IsFailure)
        {
            return Result.Failure<StartSessionResult>(programResult.Error);
        }

        var program = programResult.Value;

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

    /// <summary>
    /// Profil holatiga qarab MAJBURIY maydonlarni tekshiradi (format validatorda tekshirilgan).
    /// • `Student` yo'q — to'liq to'plam: F.I.Sh., tug'ilgan sana, jins, telefon, rozilik;
    ///   voyaga yetmagan bo'lsa ota-ona roziligi;
    /// • `Student` bor — rozilik faqat `ConsentVersion` eskirgan bo'lsa; ota-ona roziligi —
    ///   (kelgan yoki bazadagi) sana bo'yicha voyaga yetmagan VA (kelgan ?? bazadagi) qiymat
    ///   `false` bo'lsa.
    /// Kalitlar `ValidationException` chiqishidagidek camelCase — frontend `setError(key)` qiladi.
    /// </summary>
    private static Dictionary<string, string[]> RequireProfileFields(StartPublicSessionCommand request, Student? existing, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                errors["fullName"] = ["F.I.Sh. kiritilishi shart."];
            }

            if (request.BirthDate is null)
            {
                errors["birthDate"] = ["Tug'ilgan sana kiritilishi shart."];
            }

            if (request.Gender is null)
            {
                errors["gender"] = ["Jins tanlanishi shart."];
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                errors["phone"] = ["Telefon raqami kiritilishi shart."];
            }

            if (!request.ConsentAccepted)
            {
                errors["consentAccepted"] = ["Roziliksiz ro'yxatdan o'tib bo'lmaydi."];
            }
        }
        else if (existing.ConsentVersion != PublicConsent.CurrentVersion && !request.ConsentAccepted)
        {
            errors["consentAccepted"] = ["Roziliknoma matni yangilangan — davom etish uchun qayta rozilik kerak."];
        }

        var effectiveBirthDate = request.BirthDate ?? existing?.BirthDate;
        var effectiveParentalConsent = request.ParentalConsent ?? existing?.ParentalConsent ?? false;

        // Kelajakdagi sana — yosh qoidasi bu yerda tekshirilmaydi (validator allaqachon rad
        // etgan); ota-ona roziligi sharti esa bunday holatda qo'llanmaydi.
        if (effectiveBirthDate is { } birthDate
            && birthDate <= today
            && Student.CalculateAge(birthDate, today) < PublicConsent.ParentalConsentRequiredBelowAge
            && !effectiveParentalConsent)
        {
            errors["parentalConsent"] = [$"{PublicConsent.ParentalConsentRequiredBelowAge} yoshgacha bo'lganlar uchun ota-ona roziligi shart."];
        }

        return errors;
    }

    /// <summary>
    /// Mavjud `Student` ga kelgan maydonlarni tahrir sifatida qo'llaydi (`null` — o'zgarmasin).
    /// Rozilik: `ConsentAccepted = true` kelsa joriy versiya bilan qayta yoziladi; kelmasa
    /// va faqat ota-ona roziligi o'zgargan bo'lsa — versiya/sana saqlanib, bayroq yangilanadi.
    /// </summary>
    private static Result ApplyProfileChanges(StartPublicSessionCommand request, Student student, DateTimeOffset now)
    {
        var hasProfileChanges = request.FullName is not null
            || request.BirthDate is not null
            || request.Gender is not null
            || request.Phone is not null
            || request.Grade is not null
            || request.Email is not null;

        if (hasProfileChanges)
        {
            PhoneNumber phone;
            if (request.Phone is not null)
            {
                var phoneResult = PhoneNumber.Create(request.Phone);
                if (phoneResult.IsFailure)
                {
                    return Result.Failure(phoneResult.Error);
                }

                phone = phoneResult.Value;
            }
            else
            {
                phone = student.Phone;
            }

            student.UpdateProfile(
                request.FullName ?? student.FullName,
                request.BirthDate ?? student.BirthDate,
                request.Gender ?? student.Gender,
                request.Grade ?? student.Grade,
                phone,
                request.Email ?? student.Email,
                now);
        }

        var parentalConsent = request.ParentalConsent ?? student.ParentalConsent;

        if (request.ConsentAccepted)
        {
            student.RecordConsent(now, PublicConsent.CurrentVersion, parentalConsent, now);
        }
        else if (parentalConsent != student.ParentalConsent)
        {
            student.RecordConsent(student.ConsentGivenAt, student.ConsentVersion, parentalConsent, now);
        }

        return Result.Success();
    }

    private static Error ValidationFailure(Dictionary<string, string[]> errors) =>
        new(
            ProblemCodes.ValidationError,
            "Kiritilgan ma'lumotlar noto'g'ri.",
            new Dictionary<string, object> { ["errors"] = errors });

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

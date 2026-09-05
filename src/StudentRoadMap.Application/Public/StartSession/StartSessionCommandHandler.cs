using System.Security.Cryptography;
using System.Text;
using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.Common;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartSession;

/// <summary>
/// `docs/07` 1.2-bo'lim + `docs/08` 3-bo'lim + BR-1/BR-5 (`docs/02`).
///
/// Oqim:
/// 1. Maktabni topish, token (fixed-time) va faollikni tekshirish, ixtiyoriy kirish kodi.
/// 2. O'quvchini (maktab+normalizatsiyalangan FISH+tug'ilgan sana) qidirish:
///    - tugallanmagan (`Draft`/`InProgress`) muddati o'tmagan sessiya bor → shu sessiya qaytariladi (`resumed: true`);
///    - tugallanmagan sessiya muddati o'tgan → `Abandoned`ga o'tkaziladi (BR-5), yangi sessiya ochiladi;
///    - 90 kun ichida yakunlangan (`CompletedAt != null`) sessiya bor → `409 DUPLICATE_ASSESSMENT`;
///    - aks holda yangi sessiya ochiladi (mavjud o'quvchi bilan yoki yangi o'quvchi yaratib).
/// 3. Faqat shu nuqtada — **yangi `Assessment` chindan yaratilishidan oldin** — kunlik ro'yxatdan
///    o'tish hisoblagichi atomik oshiriladi; limitdan oshsa `429` (PM qarori, 2026-08-31: pastga qarang).
/// 4. Nashr qilingan/faol test bloklari (`DisplayOrder` bo'yicha) biriktiriladi.
///
/// **PM qarori (2026-08-31):** hisoblagich faqat YANGI `Assessment` yaratilganda oshiriladi —
/// `resumed: true` va `409 DUPLICATE_ASSESSMENT` holatlarida OSHIRILMAYDI. Sabab:
/// `registration_counters` maktab uchun ko'rinadigan biznes ko'rsatkich ("kunlik ro'yxatdan
/// o'tish soni", admin panelda ham chiqadi) — har so'rovni sanash uni ma'nosiz qiladi (bitta
/// o'quvchi sahifani bir necha marta yangilasa maktab limitidan bekorga o'rin yeydi, boshqa
/// o'quvchilar kira olmay qoladi). Suiiste'moldan himoya — bu rate limiting (`docs/07` 4-bo'lim,
/// `RateLimitSetup.PublicStartSession`) vazifasi, ikkisi aralashtirilmaydi.
/// </summary>
internal sealed class StartSessionCommandHandler : IRequestHandler<StartSessionCommand, Result<StartSessionResult>>
{
    private const int SessionTokenByteLength = 32;
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromDays(90);

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IIpHasher _ipHasher;
    private readonly IAppSettings _appSettings;

    public StartSessionCommandHandler(
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

    public async Task<Result<StartSessionResult>> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var school = await _executor.FirstOrDefaultAsync(
            _context.Schools.Where(s => s.Slug == SchoolSlug.FromExisting(request.Slug)),
            cancellationToken).ConfigureAwait(false);

        if (school is null || !TokensMatch(school.AccessToken, request.AccessToken))
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.NotFound, "Havola topilmadi."));
        }

        if (!school.IsActive)
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.SchoolInactive, "Ushbu maktab havolasi hozircha faol emas."));
        }

        if (!string.IsNullOrEmpty(school.AccessCode) && !TokensMatch(school.AccessCode, request.AccessCode ?? string.Empty))
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.AccessCodeInvalid, "Kirish kodi noto'g'ri."));
        }

        var phoneResult = PhoneNumber.Create(request.Phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure<StartSessionResult>(phoneResult.Error);
        }

        PhoneNumber? parentPhone = null;
        if (!string.IsNullOrWhiteSpace(request.ParentPhone))
        {
            var parentPhoneResult = PhoneNumber.Create(request.ParentPhone);
            if (parentPhoneResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(parentPhoneResult.Error);
            }

            parentPhone = parentPhoneResult.Value;
        }

        var normalizedName = NameNormalizer.Normalize(request.FullName);

        var existingStudent = await _executor.FirstOrDefaultAsync(
            _context.Students.Where(s =>
                s.SchoolId == school.Id &&
                s.NormalizedName == normalizedName &&
                s.BirthDate == request.BirthDate),
            cancellationToken).ConfigureAwait(false);

        Student student;
        var isNewStudent = existingStudent is null;

        if (existingStudent is not null)
        {
            // TODO(P30): server tomonida `OrderByDescending(DateTimeOffset)` ataylab ishlatilmayapti
            // — SQLite provayderi (integratsiya sinovlari muhiti, Docker/PostgreSQL yo'q)
            // `DateTimeOffset` bo'yicha ORDER BY'ni tarjima qila olmaydi (`NotSupportedException`).
            // Bu — texnik qarz: sinovlar Postgres'dagi haqiqiy (server-side ORDER BY) so'rov
            // yo'lini SINAMAYAPTI. BR-1 bo'yicha bitta o'quvchida amalda 0–1 ta tugallanmagan
            // sessiya bo'lgani uchun mijoz tomonida saralash hozircha xavfsiz (kichik to'plam,
            // to'g'ri natija), lekin bu faqat vaqtinchalik yechim. P30 (E2E)da Testcontainers
            // bilan haqiqiy Postgres ustida qayta tekshirilib, kerak bo'lsa server-side
            // `OrderByDescending`ga qaytariladi.
            var unfinishedCandidates = await _executor.ToListAsync(
                _context.Assessments
                    .Where(a => a.StudentId == existingStudent.Id && (a.Status == AssessmentStatus.Draft || a.Status == AssessmentStatus.InProgress)),
                cancellationToken).ConfigureAwait(false);

            var unfinished = unfinishedCandidates.OrderByDescending(a => a.StartedAt).FirstOrDefault();

            if (unfinished is not null)
            {
                if (unfinished.ExpiresAt > now)
                {
                    // Davom ettirish (`docs/07` 1.2: "tugallanmagan sessiya bor → o'sha sessiya
                    // qaytariladi, resumed: true"). Yangi `Assessment` yaratilmagani uchun kunlik
                    // hisoblagich BU YERDA OSHIRILMAYDI (PM qarori, sinf izohiga qarang).
                    var resumedTests = await BuildTestSummariesAsync(unfinished.Id, cancellationToken).ConfigureAwait(false);
                    return Result.Success(new StartSessionResult(
                        unfinished.SessionToken,
                        unfinished.Id,
                        unfinished.Status.ToString(),
                        unfinished.ExpiresAt,
                        Resumed: true,
                        resumedTests));
                }

                // BR-5: muddati o'tgan sessiya davom ettirilmaydi — tashlab ketilgan deb belgilanadi, yangi sessiya ochiladi.
                unfinished.MarkAbandoned(now);
            }

            // TODO(P30): yuqoridagi izohdagi sabab bilan bir xil — `CompletedAt >= ...` server
            // tomonida SQLite'da tarjima qilinmaydi, shu sabab mijoz tomonida filtrlanadi.
            var completedAssessments = await _executor.ToListAsync(
                _context.Assessments.Where(a => a.StudentId == existingStudent.Id && a.CompletedAt != null),
                cancellationToken).ConfigureAwait(false);

            var hasRecentCompleted = completedAssessments.Any(a => a.CompletedAt!.Value >= now - DuplicateWindow);

            if (hasRecentCompleted)
            {
                // Yangi `Assessment` yaratilmagani uchun kunlik hisoblagich BU YERDA HAM OSHIRILMAYDI.
                return Result.Failure<StartSessionResult>(new Error(
                    ProblemCodes.DuplicateAssessment,
                    "Ushbu o'quvchi so'nggi 90 kun ichida testni allaqachon yakunlagan."));
            }

            student = existingStudent;
        }
        else
        {
            student = Student.Create(
                Guid.NewGuid(),
                school.Id,
                request.FullName,
                request.BirthDate,
                request.Gender,
                request.Grade,
                phoneResult.Value,
                consentGivenAt: now,
                now: now,
                classLetter: request.ClassLetter,
                parentPhone: parentPhone,
                email: request.Email);
        }

        // `docs/06` 8-bo'lim (2026-09-02 qaror), `prompts/34` C9-band: dastur tanlanadi —
        // bo'sh `programCode` + bitta mavjud dastur bo'lsa avtomatik, aks holda aniq
        // ko'rsatilishi shart. Muvaffaqiyatsiz bo'lsa (`400`/`404`) kunlik hisoblagich
        // OSHIRILMAYDI (pastdagi "BR-1" izohi bilan bir xil qoida — assessment hali yaratilmadi).
        var programResult = await ResolveProgramAsync(school.Id, request.ProgramCode, cancellationToken).ConfigureAwait(false);
        if (programResult.IsFailure)
        {
            return Result.Failure<StartSessionResult>(programResult.Error);
        }

        var program = programResult.Value;

        // BR-1 kunlik ro'yxatdan o'tish limiti — FAQAT shu nuqtadan boshlab, ya'ni yangi
        // `Assessment` chindan yaratilishidan oldin, atomik oshiriladi (PM qarori, sinf
        // izohiga qarang: `resumed`/`409` holatlarida hisoblagich o'zgarmaydi).
        var dateUtc = DateOnly.FromDateTime(now.UtcDateTime);
        var registrationCount = await _context
            .IncrementRegistrationCounterAsync(school.Id, dateUtc, cancellationToken)
            .ConfigureAwait(false);

        if (registrationCount > school.DailyRegistrationLimit)
        {
            return Result.Failure<StartSessionResult>(new Error(ProblemCodes.RateLimited, "Ushbu maktab uchun kunlik ro'yxatdan o'tish limiti tugadi."));
        }

        var sessionToken = _tokenGenerator.GenerateUrlSafeToken(SessionTokenByteLength);
        var expiresAt = now.AddDays(_appSettings.SessionLifetimeDays);
        var ipHash = _ipHasher.Hash(request.IpAddress);

        var assessment = Assessment.Create(
            Guid.NewGuid(),
            student.Id,
            school.Id,
            sessionToken,
            string.IsNullOrWhiteSpace(request.LanguageCode) ? "uz" : request.LanguageCode,
            program.Id,
            startedAt: now,
            expiresAt: expiresAt,
            now: now,
            ipHash: ipHash,
            userAgent: request.UserAgent);

        // Sessiyaga FAQAT shu dasturning testlari, DASTURDAGI tartibda qo'shiladi (`prompts/34`
        // C10-band) — `TestDefinition.DisplayOrder` EMAS, `ProgramTest.DisplayOrder`. Mantiq
        // `AssessmentTestAttacher` ga ko'chirildi (P47): ommaviy (maktabsiz) oqim ham AYNAN
        // shu qadamni bajaradi, nusxa ko'chirilgan kod ikki oqimda ajralib ketishi mumkin edi.
        var tests = await AssessmentTestAttacher.AttachAsync(_context, _executor, assessment, program.Id, cancellationToken)
            .ConfigureAwait(false);

        if (isNewStudent)
        {
            _context.Add(student);
        }

        _context.Add(assessment);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new StartSessionResult(
            sessionToken,
            assessment.Id,
            assessment.Status.ToString(),
            expiresAt,
            Resumed: false,
            tests);

        return Result.Success(result);
    }

    private async Task<IReadOnlyList<PublicTestSummaryDto>> BuildTestSummariesAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var assessmentTests = await _executor.ToListAsync(
            _context.AssessmentTests.Where(t => t.AssessmentId == assessmentId).OrderBy(t => t.DisplayOrder),
            cancellationToken).ConfigureAwait(false);

        var testDefinitionIds = assessmentTests.Select(t => t.TestDefinitionId).ToList();
        var definitionsByTestDefinitionId = await _executor.ToListAsync(
            _context.TestDefinitions
                .Where(t => testDefinitionIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Code, t.NameUz, t.EstimatedMinutes }),
            cancellationToken).ConfigureAwait(false);

        var definitionLookup = definitionsByTestDefinitionId.ToDictionary(x => x.Id, x => x);

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
    /// Dastur tanlash mantiqi (`docs/06` 8-bo'lim, 2026-09-02 qaror, `prompts/34` C9-band):
    /// - `programCode` bo'sh va maktabda aynan bitta mavjud dastur bo'lsa — o'sha tanlanadi;
    /// - bir nechta bo'lsa va `programCode` berilmasa — `400 PROGRAM_REQUIRED`;
    /// - berilgan `programCode` shu maktabda mavjud (`ProgramAvailability`) bo'lmasa — `404`
    ///   (boshqa maktabga biriktirilgan yoki umuman mavjud bo'lmagan dastur bir xil javob
    ///   beradi — mavjudligini oshkor qilmaslik uchun, `GetSchoolInfoQueryHandler` uslubida).
    /// </summary>
    private async Task<Result<AssessmentProgram>> ResolveProgramAsync(Guid schoolId, string? programCode, CancellationToken cancellationToken)
    {
        var availablePrograms = await ProgramAvailability.GetAvailableProgramsAsync(_context, _executor, schoolId, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(programCode))
        {
            if (availablePrograms.Count == 1)
            {
                return Result.Success(availablePrograms[0]);
            }

            var message = availablePrograms.Count == 0
                ? "Ushbu maktabga hech qanday dastur biriktirilmagan."
                : "Maktabda bir nechta dastur mavjud — dastur tanlanishi shart.";

            return Result.Failure<AssessmentProgram>(new Error(ProblemCodes.ProgramRequired, message));
        }

        var matched = availablePrograms.FirstOrDefault(p => p.Code == programCode);
        if (matched is null)
        {
            return Result.Failure<AssessmentProgram>(new Error(ProblemCodes.NotFound, "Dastur topilmadi."));
        }

        return Result.Success(matched);
    }

    /// <summary>Fixed-time solishtirish (`docs/08` 3-bo'lim) — havola tokeni va kirish kodi uchun.</summary>
    private static bool TokensMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

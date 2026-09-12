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
/// 2. O'quvchini (maktab+normalizatsiyalangan FISH+tug'ilgan sana) qidirish.
/// 3. Dastur tanlanadi (`ResolveProgramAsync`: bitta bo'lsa avtomatik, aks holda `programCode`
///    shart — `400 PROGRAM_REQUIRED`/`404`). Bu qadam sessiya holati tekshiruvidan OLDIN —
///    BR-1/BR-5 **`(o'quvchi, dastur)` juftligi** bo'yicha ishlaydi (egasining qarori,
///    2026-09-07; `ProgramSessionPolicy`), dastur tanlanmaguncha nimani tekshirish noma'lum.
/// 4. Mavjud o'quvchi bo'lsa, TANLANGAN DASTUR bo'yicha:
///    - tugallanmagan (`Draft`/`InProgress`) muddati o'tmagan sessiya bor → shu sessiya qaytariladi (`resumed: true`);
///    - tugallanmagan sessiya muddati o'tgan → `Abandoned`ga o'tkaziladi (BR-5), yangi sessiya ochiladi;
///    - 90 kun ichida yakunlangan (`CompletedAt != null`) sessiya bor → `409 DUPLICATE_ASSESSMENT`;
///    - aks holda yangi sessiya ochiladi. BOSHQA dasturdagi sessiyalar (tugallanmagan yoki
///      yakunlangan) bu qarorga ta'sir qilmaydi — A da yarim qolgan bo'lsa ham B yangi ochiladi.
/// 5. Faqat shu nuqtada — **yangi `Assessment` chindan yaratilishidan oldin** — kunlik ro'yxatdan
///    o'tish hisoblagichi atomik oshiriladi; limitdan oshsa `429` (PM qarori, 2026-08-31: pastga qarang).
/// 6. Dastur test bloklari (`ProgramTest.DisplayOrder` bo'yicha) biriktiriladi.
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

        // `docs/06` 8-bo'lim (2026-09-02 qaror), `prompts/34` C9-band: dastur tanlanadi —
        // bo'sh `programCode` + bitta mavjud dastur bo'lsa avtomatik, aks holda aniq
        // ko'rsatilishi shart. P52 (2026-09-11): bu qadam endi shaxs maydonlarini
        // O'QISHDAN OLDIN turadi — `RegistrationMode.None` dasturda ular umuman kerak emas
        // (pastga qarang), shu sabab qaysi rejim ekanini bilmasdan ularni tekshirib bo'lmaydi.
        // Muvaffaqiyatsiz bo'lsa (`400`/`404`) kunlik hisoblagich OSHIRILMAYDI (assessment
        // hali yaratilmadi) — `RegistrationMode`dan qat'i nazar.
        var programResult = await ResolveProgramAsync(school.Id, request.ProgramCode, cancellationToken).ConfigureAwait(false);
        if (programResult.IsFailure)
        {
            return Result.Failure<StartSessionResult>(programResult.Error);
        }

        var program = programResult.Value;

        // P52 (`RegistrationMode.None`, egasining 2026-09-11 qarori): registratsiya ekrani
        // UMUMAN ko'rsatilmagan — o'quvchi ANONIM yaratiladi, BR-1/BR-5 (identifikatorga
        // tayanadi) va "davom ettirish" (identifikator orqali qidiruv) SHU SABAB ishlamaydi
        // (qabul qilingan cheklov — bir xil brauzerdan bir necha marta kirish mumkin).
        // Domen invarianti (`AssessmentProgram.SetRegistrationMode`/`Publish`) bu rejimda
        // shaxsiyat batareyasi BO'LMASLIGINI kafolatlaydi.
        if (program.RegistrationMode == RegistrationMode.None)
        {
            return await HandleAnonymousAsync(school, program, request, now, cancellationToken).ConfigureAwait(false);
        }

        // `RegistrationMode.Full` — pastdagi oqim BAYT-BAYT o'zgarmagan (regressiya bilan
        // qulflangan, `PublicSessionContractRegressionTests`) standart GLOBAL sozlama bilan
        // (`birthDate`/`grade`/`phone`/`gender` majburiy, qolgani ixtiyoriy). P52 2-to'lqin
        // (2026-09-12, `docs/18` §9.6.2): manba endi `AssessmentProgram.RegistrationFields`
        // (§9.5, eskirgan — BOSHQA O'QILMAYDI) EMAS, GLOBAL `RegistrationFormSettings`, dastur
        // ustunligi qo'llangan holda (`RegistrationFormResolver`: batareya bor dasturda
        // `birthDate`/`grade` doim `Required`). Shaxs maydonlari validator darajasida
        // optsional (`StartSessionCommandValidator`), shu sabab MAJBURIYLIK shu yerda.
        var effectiveDefinition = await RegistrationFormResolver
            .GetEffectiveDefinitionForProgramAsync(_context, _executor, program.Id, cancellationToken)
            .ConfigureAwait(false);

        var fields = RegistrationFieldsMapping.FromCoreFields(effectiveDefinition.CoreFields);

        var requiredFieldsError = ValidateRequiredIdentityFields(request, fields);
        if (requiredFieldsError is not null)
        {
            return Result.Failure<StartSessionResult>(requiredFieldsError);
        }

        // P52 2-to'lqin: "o'z maydonlari" javoblari — maktab oqimida HAR SAFAR tekshiriladi
        // (boshqa asosiy maydonlar kabi, mavjud o'quvchi topilgan holatda ham — `Handle` pastki
        // qismidagi izohga qarang: topilgan o'quvchida BOSHQA maydonlar ham xuddi shunday
        // e'tiborsiz qoldiriladi, faqat qidiruv uchun ishlatiladi).
        var (customFieldErrors, validatedCustomFields) = RegistrationCustomFieldAnswers.Validate(
            effectiveDefinition.CustomFields, request.CustomFields, requireMandatory: true);

        if (customFieldErrors.Count > 0)
        {
            return Result.Failure<StartSessionResult>(new Error(
                ProblemCodes.ValidationError,
                "Kiritilgan ma'lumotlar noto'g'ri.",
                new Dictionary<string, object> { ["errors"] = customFieldErrors }));
        }

        // P52 kengaytmasi (`docs/18` §9.5): `Hidden` maydon uchun kelgan qiymat E'TIBORSIZ
        // qoldiriladi — mijoz baribir yuborsa ham saqlanmaydi (`RegistrationMode.None`dagi
        // "e'tiborsiz qoldirish" naqshi bilan bir xil, faqat maydon darajasida).
        DateOnly? effectiveBirthDate = fields.BirthDate == RegistrationFieldRequirement.Hidden ? null : request.BirthDate;
        Gender? effectiveGender = fields.Gender == RegistrationFieldRequirement.Hidden ? null : request.Gender;
        int? effectiveGrade = fields.Grade == RegistrationFieldRequirement.Hidden ? null : request.Grade;
        string? effectiveClassLetter = fields.ClassLetter == RegistrationFieldRequirement.Hidden ? null : request.ClassLetter;
        string? effectivePhoneRaw = fields.Phone == RegistrationFieldRequirement.Hidden ? null : request.Phone;
        string? effectiveParentPhoneRaw = fields.ParentPhone == RegistrationFieldRequirement.Hidden ? null : request.ParentPhone;
        string? effectiveEmail = fields.Email == RegistrationFieldRequirement.Hidden ? null : request.Email;

        PhoneNumber? phone = null;
        if (!string.IsNullOrWhiteSpace(effectivePhoneRaw))
        {
            var phoneResult = PhoneNumber.Create(effectivePhoneRaw);
            if (phoneResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(phoneResult.Error);
            }

            phone = phoneResult.Value;
        }

        PhoneNumber? parentPhone = null;
        if (!string.IsNullOrWhiteSpace(effectiveParentPhoneRaw))
        {
            var parentPhoneResult = PhoneNumber.Create(effectiveParentPhoneRaw);
            if (parentPhoneResult.IsFailure)
            {
                return Result.Failure<StartSessionResult>(parentPhoneResult.Error);
            }

            parentPhone = parentPhoneResult.Value;
        }

        var normalizedName = NameNormalizer.Normalize(request.FullName!);

        // O'quvchini topish kaliti — MAVJUD BO'LGAN ENG KUCHLI identifikator bo'yicha
        // (P52 kod ko'rigi tuzatmasi, 2026-09-11):
        //
        //   1. FISH + tug'ilgan sana — eng kuchli, standart sozlamadagi yo'l;
        //   2. FISH + telefon — tug'ilgan sana yo'q bo'lsa (`Optional`/`Hidden`). Bir xil ism
        //      VA bir xil telefon amalda bitta odam;
        //   3. ikkalasi ham yo'q — qidiruvsiz, har doim yangi yozuv.
        //
        // NEGA MUHIM: bu qidiruv faqat takrorlanishni aniqlash (BR-1) uchun emas, sessiyani
        // TIKLASH (BR-5, `resumed: true`) uchun HAM ishlatiladi. Ilgari bu yerda `birthDate`
        // yo'q bo'lsa qidiruv BUTUNLAY o'chirilardi — natijada o'quvchi qaytib kelganda yarim
        // qolgan sessiyasi topilmay, noldan yangi sessiya boshlanardi va javoblari yo'qolardi.
        // Bu takroriy yozuvdan YOMONROQ. Uchinchi holat (hech qanday kalit yo'q) ataylab
        // qidiruvsiz qoladi: faqat ism bo'yicha izlash bir xil ismli ikki o'quvchini bitta
        // yozuvga qo'shib yuborardi (ma'lumot buzilishi).
        //
        // Anonim yozuvlar (`RegistrationMode.None`) qidiruvdan ataylab chiqariladi — ularning
        // ismi generatsiya qilingan va hech qachon haqiqiy o'quvchiga mos kelmasligi kerak.
        Student? existingStudent = null;
        if (effectiveBirthDate is not null)
        {
            existingStudent = await _executor.FirstOrDefaultAsync(
                _context.Students.Where(s =>
                    s.SchoolId == school.Id &&
                    !s.IsAnonymous &&
                    s.NormalizedName == normalizedName &&
                    s.BirthDate == effectiveBirthDate),
                cancellationToken).ConfigureAwait(false);
        }
        else if (phone is not null)
        {
            existingStudent = await _executor.FirstOrDefaultAsync(
                _context.Students.Where(s =>
                    s.SchoolId == school.Id &&
                    !s.IsAnonymous &&
                    s.NormalizedName == normalizedName &&
                    s.Phone == phone),
                cancellationToken).ConfigureAwait(false);
        }

        Student student;
        var isNewStudent = existingStudent is null;

        if (existingStudent is not null)
        {
            // Faqat TANLANGAN dasturdagi tugallanmagan sessiya — boshqa dasturdagi yarim qolgan
            // sessiya bu dasturni boshlashga to'sqinlik qilmaydi (`ProgramSessionPolicy`).
            var unfinished = await ProgramSessionPolicy
                .FindUnfinishedAsync(_context, _executor, existingStudent.Id, program.Id, cancellationToken)
                .ConfigureAwait(false);

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

            // BR-1 — shu DASTUR bo'yicha 90 kunlik oyna (boshqa dasturni yakunlagani hisobga olinmaydi).
            var hasRecentCompleted = await ProgramSessionPolicy
                .HasCompletedWithinWindowAsync(_context, _executor, existingStudent.Id, program.Id, now, cancellationToken)
                .ConfigureAwait(false);

            if (hasRecentCompleted)
            {
                // Yangi `Assessment` yaratilmagani uchun kunlik hisoblagich BU YERDA HAM OSHIRILMAYDI.
                return Result.Failure<StartSessionResult>(new Error(
                    ProblemCodes.DuplicateAssessment,
                    "Ushbu o'quvchi so'nggi 90 kun ichida bu dasturni allaqachon yakunlagan."));
            }

            student = existingStudent;
        }
        else
        {
            student = Student.Create(
                Guid.NewGuid(),
                school.Id,
                request.FullName!,
                effectiveBirthDate,
                effectiveGender ?? Gender.Unspecified,
                effectiveGrade ?? Student.NoGrade,
                phone,
                consentGivenAt: now,
                now: now,
                classLetter: effectiveClassLetter,
                parentPhone: parentPhone,
                email: effectiveEmail,
                profileExtra: RegistrationCustomFieldAnswers.Serialize(validatedCustomFields));
        }

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

    /// <summary>
    /// P52 (`RegistrationMode.None`): registratsiya ekrani ko'rsatilmagan dastur uchun ANONIM
    /// oqim — shaxs maydonlari (kelgan bo'lsa ham) E'TIBORSIZ qoldiriladi, o'quvchi HAR DOIM
    /// yangi (`Student.CreateAnonymous`) yaratiladi (identifikator yo'qligi sabab
    /// BR-1/BR-5/"davom ettirish" ishlamaydi — qabul qilingan cheklov, `Handle` izohi).
    /// Qolgan qadamlar (kunlik limit, sessiya, test biriktirish) `Full` yo'li bilan bir xil.
    /// </summary>
    private async Task<Result<StartSessionResult>> HandleAnonymousAsync(
        School school,
        AssessmentProgram program,
        StartSessionCommand request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var student = Student.CreateAnonymous(Guid.NewGuid(), school.Id, consentGivenAt: now, now: now);

        // BR-1 kunlik ro'yxatdan o'tish limiti — `Full` yo'li bilan bir xil qoida
        // (`Handle` izohi: faqat YANGI `Assessment` yaratilishidan oldin oshiriladi;
        // anonim oqimda HAR so'rov yangi `Assessment` yaratadi, shu sabab har doim oshadi).
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

        var tests = await AssessmentTestAttacher.AttachAsync(_context, _executor, assessment, program.Id, cancellationToken)
            .ConfigureAwait(false);

        _context.Add(student);
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

    /// <summary>
    /// `RegistrationMode.Full` dasturda `fullName` HAR DOIM majburiy (bu maydon
    /// `RegistrationFields`da YO'Q, `RegistrationFields.cs` izohiga qarang). Qolgan
    /// maydonlarning majburiyligi DASTURNING `RegistrationFields` sozlamasidan kelib chiqadi
    /// (P52 kengaytmasi, `docs/18` §9.5) — bu tekshiruv DB'ga bog'liq bo'lgani (dastur
    /// sozlamasi) sabab shu yerda, `StartSessionCommandValidator` emas (`Handle` izohi).
    /// Standart sozlamada (`RegistrationFields.Default`) xabarlar VA xatti-harakat eski
    /// (2026-09-11 gacha bo'lgan) validator bilan AYNAN bir xil — kalitlar
    /// (`fullName`/`birthDate`/`grade`/`phone`) `docs/07` shartnomasi bilan mos.
    /// </summary>
    private static Error? ValidateRequiredIdentityFields(StartSessionCommand request, RegistrationFields fields)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors["fullName"] = ["F.I.Sh. kiritilishi shart."];
        }

        if (fields.BirthDate == RegistrationFieldRequirement.Required && request.BirthDate is null)
        {
            errors["birthDate"] = ["Tug'ilgan sana kiritilishi shart."];
        }

        // `Gender.Unspecified` ni ham "kiritilmagan" deb hisoblaymiz — `null` mijoz maydonni
        // umuman yubormaganda, `Unspecified` esa forma "tanlanmagan" holatini aniq qiymat
        // sifatida yuborsa ham yuz beradi; ikkalasi ham `Required` uchun bo'sh hisoblanadi.
        if (fields.Gender == RegistrationFieldRequirement.Required &&
            (request.Gender is null || request.Gender == Gender.Unspecified))
        {
            errors["gender"] = ["Jinsni tanlang."];
        }

        if (fields.Grade == RegistrationFieldRequirement.Required && request.Grade is null)
        {
            errors["grade"] = ["Sinf kiritilishi shart."];
        }

        if (fields.ClassLetter == RegistrationFieldRequirement.Required && string.IsNullOrWhiteSpace(request.ClassLetter))
        {
            errors["classLetter"] = ["Sinf harfi kiritilishi shart."];
        }

        if (fields.Phone == RegistrationFieldRequirement.Required && string.IsNullOrWhiteSpace(request.Phone))
        {
            errors["phone"] = ["Telefon raqami kiritilishi shart."];
        }

        if (fields.ParentPhone == RegistrationFieldRequirement.Required && string.IsNullOrWhiteSpace(request.ParentPhone))
        {
            errors["parentPhone"] = ["Ota-ona telefon raqami kiritilishi shart."];
        }

        if (fields.Email == RegistrationFieldRequirement.Required && string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email kiritilishi shart."];
        }

        if (errors.Count == 0)
        {
            return null;
        }

        return new Error(
            ProblemCodes.ValidationError,
            "Kiritilgan ma'lumotlar noto'g'ri.",
            new Dictionary<string, object> { ["errors"] = errors });
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

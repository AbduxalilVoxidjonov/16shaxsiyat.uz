using MediatR;
using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Admin.Common;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Identity;
using StudentRoadMap.Domain.Schools;

namespace StudentRoadMap.Application.Admin.Schools.Create;

/// <summary>
/// `docs/07` 3.1-bo'lim + `prompts/14` MAXSUS DIQQAT #3 (slug generatsiyasi va poyga holati).
///
/// **Slug:** `Name` + `District` → translit (`SchoolSlug`). Band bo'lsa `-2`, `-3`, ... bilan
/// birinchi BO'SH variant PROAKTIV tarzda (DB'dan tekshirib) tanlanadi — bu holatlarning
/// ko'pchiligini (ikki so'rov orasida real vaqt farqi bo'lganda) yopadi. ChIN bir vaqtdagi
/// (ikkala so'rov ORALIQ tekshiruv bilan yakuniy `INSERT` orasida) poyga holati esa
/// `ux_schools_slug` unique indeksi orqali DB darajasida ushlanadi — `AppDbContext.SaveChangesAsync`
/// buni `UniqueConstraintViolationException`ga aylantiradi, `ExceptionHandlingMiddleware`
/// esa tushunarli `409 UNIQUE_CONSTRAINT_CONFLICT` qaytaradi (jimgina `500` EMAS).
///
/// **`AccessToken`** generatsiyasida ATAYLAB qayta urinish/tekshiruv YO'Q — 32 bayt tasodifiy
/// qiymatning to'qnashish ehtimoli amalda nolga teng (`docs/08` 3-bo'lim), `ux_schools_token`
/// baribir DB darajasida himoya qiladi.
///
/// **`EntryCode`** (maktab kodi, `docs/08` 3a) — yaratishda AVTOMATIK, admin kiritmaydi.
/// 8 belgi qisqa bo'lgani uchun `SchoolEntryCodeAllocator` DB'dan tekshirib bo'sh kod tanlaydi;
/// `ux_schools_entry_code` yakuniy himoya. Javobda (`AdminSchoolDetailDto.EntryCode`) darhol
/// ko'rinadi — admin uni maktabga havola/QR bilan birga beradi.
/// </summary>
internal sealed class CreateSchoolCommandHandler : IRequestHandler<CreateSchoolCommand, Result<AdminSchoolDetailDto>>
{
    private const int AccessTokenByteLength = 32;
    private const int MaxSlugAttempts = 50;

    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IDateTime _dateTime;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IEntryCodeGenerator _entryCodeGenerator;
    private readonly IAppSettings _appSettings;
    private readonly IIpHasher _ipHasher;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public CreateSchoolCommandHandler(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        IDateTime dateTime,
        ITokenGenerator tokenGenerator,
        IEntryCodeGenerator entryCodeGenerator,
        IAppSettings appSettings,
        IIpHasher ipHasher,
        IQrCodeGenerator qrCodeGenerator)
    {
        _context = context;
        _executor = executor;
        _dateTime = dateTime;
        _tokenGenerator = tokenGenerator;
        _entryCodeGenerator = entryCodeGenerator;
        _appSettings = appSettings;
        _ipHasher = ipHasher;
        _qrCodeGenerator = qrCodeGenerator;
    }

    public async Task<Result<AdminSchoolDetailDto>> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTime.UtcNow;

        var slugResult = await AllocateUniqueSlugAsync(request.Name, request.District, cancellationToken).ConfigureAwait(false);
        if (slugResult.IsFailure)
        {
            return Result.Failure<AdminSchoolDetailDto>(slugResult.Error);
        }

        var accessToken = _tokenGenerator.GenerateUrlSafeToken(AccessTokenByteLength);
        var entryCode = await SchoolEntryCodeAllocator
            .AllocateUniqueAsync(_context, _executor, _entryCodeGenerator, cancellationToken)
            .ConfigureAwait(false);

        var school = School.Create(
            Guid.NewGuid(),
            request.Name,
            request.Region,
            request.District,
            slugResult.Value,
            accessToken,
            entryCode,
            now,
            schoolNumber: request.SchoolNumber,
            contactPerson: request.ContactPerson,
            contactPhone: request.ContactPhone,
            accessCode: request.AccessCode,
            dailyRegistrationLimit: request.DailyRegistrationLimit ?? 500,
            notes: request.Notes);

        _context.Add(school);

        if (request.TestIds is { Count: > 0 })
        {
            // 2026-09-23 (`docs/18` §9.7): maktab formasida endi TESTLAR tanlanadi.
            var assignResult = await SchoolTestAssignment
                .ReplaceAsync(_context, _executor, school.Id, request.TestIds, request.AdminUserId, now, cancellationToken)
                .ConfigureAwait(false);

            if (assignResult.IsFailure)
            {
                return Result.Failure<AdminSchoolDetailDto>(assignResult.Error);
            }
        }

        // Audit'da faqat institutsional (shaxsiy bo'lmagan) maydonlar — `CLAUDE.md` 6-band
        // ruhi (o'quvchi PII haqida) maktabga ham qo'llanildi: `ContactPerson`/`ContactPhone`
        // audit'ga yozilmaydi.
        _context.Add(AuditLog.Create(
            AuditActions.SchoolCreated,
            now,
            request.AdminUserId,
            entityType: "School",
            entityId: school.Id,
            afterJson: AuditSnapshot.Serialize(new { school.Id, school.Name, Slug = school.Slug.Value, school.Region, school.District, school.IsActive, request.TestIds }),
            ipHash: _ipHasher.Hash(request.IpAddress),
            userAgent: request.UserAgent));

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Yangi maktabga hali dastur biriktirilmagan — havola sog'ligi `Public` dastur bor-yo'qligiga
        // bog'liq, shu sabab shu yerda ham hisoblanadi (admin darhol "havola ishlamaydi" belgisini ko'rsin).
        var linkHealth = await SchoolLinkHealthEvaluator.EvaluateOneAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);

        var testIds = await SchoolTestAssignment.GetTestIdsAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);

        var dto = SchoolMapping.ToDetailDto(school, _appSettings, _qrCodeGenerator, new AdminSchoolStatsDto(0, 0, 0, null, null), linkHealth, testIds);

        return Result.Success(dto);
    }

    private async Task<Result<SchoolSlug>> AllocateUniqueSlugAsync(string name, string district, CancellationToken cancellationToken)
    {
        var baseResult = SchoolSlug.Create($"{name} {district}");
        if (baseResult.IsFailure)
        {
            return baseResult;
        }

        var baseSlug = baseResult.Value.Value;

        for (var suffix = 1; suffix <= MaxSlugAttempts; suffix++)
        {
            var candidate = BuildCandidate(baseSlug, suffix);

            var exists = await _executor.AnyAsync(
                _context.Schools.Where(s => s.Slug == SchoolSlug.FromExisting(candidate)),
                cancellationToken).ConfigureAwait(false);

            if (!exists)
            {
                return Result.Success(SchoolSlug.FromExisting(candidate));
            }
        }

        return Result.Failure<SchoolSlug>(new Error(
            ProblemCodes.UniqueConstraintConflict,
            $"'{baseSlug}' uchun bo'sh havola manzili topilmadi ({MaxSlugAttempts} urinishdan keyin)."));
    }

    private static string BuildCandidate(string baseSlug, int suffix)
    {
        if (suffix == 1)
        {
            return baseSlug;
        }

        var suffixText = $"-{suffix}";
        var maxBaseLength = SchoolSlug.MaxLength - suffixText.Length;
        var trimmedBase = baseSlug.Length > maxBaseLength ? baseSlug[..maxBaseLength].TrimEnd('-') : baseSlug;

        return trimmedBase + suffixText;
    }
}

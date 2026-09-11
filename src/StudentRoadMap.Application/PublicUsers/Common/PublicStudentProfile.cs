using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.Public.StartPublicSession;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.Common;

/// <summary>
/// Ommaviy foydalanuvchi anketasi (`Student` ↔ `PublicUserId`) ustidagi UMUMIY amallar.
/// Ikki handler foydalanadi: `StartPublicSessionCommandHandler` (profil + sessiya) va
/// `UpdateStudentProfileCommandHandler` (FAQAT profil). 2026-09-07 gacha bu mantiq sessiya
/// handlerining xususiy metodlari edi; "O'zgartirish" bosgan foydalanuvchida test boshlanib
/// ketgani sabab faqat saqlaydigan endpoint kerak bo'ldi — qoidalar esa ikkalasida AYNAN
/// bir xil bo'lishi shart (`docs/07` §5.4 jadvali), shu sabab bu yerga chiqarildi.
///
/// Hech biri `SaveChangesAsync` chaqirmaydi — tranzaksiya chegarasi handlerda.
/// </summary>
internal static class PublicStudentProfile
{
    /// <summary>
    /// Ommaviy makon (`SchoolKind.PublicSpace`) — bazada AYNAN BITTA (`ux_schools_public_space`
    /// qisman unikal indeks). `Id` konstantasi bo'yicha emas, TUR bo'yicha qidiriladi:
    /// `DbSeeder.PublicSpaceSchoolId` `Infrastructure` qatlamida va `Application` unga
    /// bog'lana olmaydi (`docs/06` 3-bo'lim). `null` — seed bajarilmagan
    /// (`PUBLIC_SPACE_NOT_CONFIGURED`).
    /// </summary>
    public static Task<School?> FindPublicSpaceAsync(IAppDbContext context, IAsyncQueryExecutor executor, CancellationToken cancellationToken) =>
        executor.FirstOrDefaultAsync(context.Schools.Where(s => s.Kind == SchoolKind.PublicSpace), cancellationToken);

    /// <summary>Shu akkauntning ommaviy makondagi `Student` yozuvi (kuzatuvda — tahrir uchun).</summary>
    public static Task<Student?> FindStudentAsync(
        IAppDbContext context,
        IAsyncQueryExecutor executor,
        Guid spaceId,
        Guid publicUserId,
        CancellationToken cancellationToken) =>
        executor.FirstOrDefaultAsync(
            context.Students.Where(s => s.SchoolId == spaceId && s.PublicUserId == publicUserId),
            cancellationToken);

    /// <summary>
    /// Profil holatiga qarab MAJBURIY maydonlarni tekshiradi (format validatorda tekshirilgan):
    /// • `Student` yo'q — to'liq to'plam: F.I.Sh., tug'ilgan sana, jins, telefon, rozilik;
    ///   voyaga yetmagan bo'lsa ota-ona roziligi;
    /// • `Student` bor — rozilik faqat `ConsentVersion` eskirgan bo'lsa; ota-ona roziligi —
    ///   (kelgan yoki bazadagi) sana bo'yicha voyaga yetmagan VA (kelgan ?? bazadagi) qiymat
    ///   `false` bo'lsa.
    /// Kalitlar `ValidationException` chiqishidagidek camelCase — frontend `setError(key)` qiladi.
    /// </summary>
    public static Dictionary<string, string[]> RequireFields(IPublicProfileInput input, Student? existing, DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(input.FullName))
            {
                errors["fullName"] = ["F.I.Sh. kiritilishi shart."];
            }

            if (input.BirthDate is null)
            {
                errors["birthDate"] = ["Tug'ilgan sana kiritilishi shart."];
            }

            if (input.Gender is null)
            {
                errors["gender"] = ["Jins tanlanishi shart."];
            }

            if (string.IsNullOrWhiteSpace(input.Phone))
            {
                errors["phone"] = ["Telefon raqami kiritilishi shart."];
            }

            if (!input.ConsentAccepted)
            {
                errors["consentAccepted"] = ["Roziliksiz ro'yxatdan o'tib bo'lmaydi."];
            }
        }
        else if (existing.ConsentVersion != PublicConsent.CurrentVersion && !input.ConsentAccepted)
        {
            errors["consentAccepted"] = ["Roziliknoma matni yangilangan — davom etish uchun qayta rozilik kerak."];
        }

        var effectiveBirthDate = input.BirthDate ?? existing?.BirthDate;
        var effectiveParentalConsent = input.ParentalConsent ?? existing?.ParentalConsent ?? false;

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
    public static Result ApplyChanges(IPublicProfileInput input, Student student, DateTimeOffset now)
    {
        var hasProfileChanges = input.FullName is not null
            || input.BirthDate is not null
            || input.Gender is not null
            || input.Phone is not null
            || input.Grade is not null
            || input.Email is not null;

        if (hasProfileChanges)
        {
            PhoneNumber phone;
            if (input.Phone is not null)
            {
                var phoneResult = PhoneNumber.Create(input.Phone);
                if (phoneResult.IsFailure)
                {
                    return Result.Failure(phoneResult.Error);
                }

                phone = phoneResult.Value;
            }
            else
            {
                // Ommaviy foydalanuvchi oqimi anonim (`RegistrationMode.None`) yaratmaydi —
                // bu yerga yetib kelgan `Student` doim to'liq profilli (`Phone` != null).
                phone = student.Phone!;
            }

            student.UpdateProfile(
                input.FullName ?? student.FullName,
                input.BirthDate ?? student.BirthDate!.Value,
                input.Gender ?? student.Gender,
                input.Grade ?? student.Grade,
                phone,
                input.Email ?? student.Email,
                now);
        }

        var parentalConsent = input.ParentalConsent ?? student.ParentalConsent;

        if (input.ConsentAccepted)
        {
            student.RecordConsent(now, PublicConsent.CurrentVersion, parentalConsent, now);
        }
        else if (parentalConsent != student.ParentalConsent)
        {
            student.RecordConsent(student.ConsentGivenAt, student.ConsentVersion, parentalConsent, now);
        }

        return Result.Success();
    }

    /// <summary>
    /// Ommaviy makonda YANGI `Student` yaratadi (`PublicUserId` bilan bog'langan, rozilik joriy
    /// versiya bilan). Chaqiruvchi <see cref="RequireFields"/> ni OLDIN o'tkazgan bo'lishi
    /// shart — shu sabab `!`/`.Value` xavfsiz. Kontekstga QO'SHILMAYDI — chaqiruvchi
    /// `context.Add(student)` qiladi (sessiya handleri buni `Assessment` bilan birga qiladi).
    /// </summary>
    public static Result<Student> CreateStudent(IPublicProfileInput input, Guid spaceId, Guid publicUserId, DateTimeOffset now)
    {
        var phoneResult = PhoneNumber.Create(input.Phone!);
        if (phoneResult.IsFailure)
        {
            return Result.Failure<Student>(phoneResult.Error);
        }

        var student = Student.Create(
            Guid.NewGuid(),
            spaceId,
            input.FullName!,
            input.BirthDate!.Value,
            input.Gender!.Value,
            input.Grade ?? Student.NoGrade,
            phoneResult.Value,
            consentGivenAt: now,
            now: now,
            email: string.IsNullOrWhiteSpace(input.Email) ? null : input.Email,
            publicUserId: publicUserId,
            consentVersion: PublicConsent.CurrentVersion,
            parentalConsent: input.ParentalConsent ?? false);

        return Result.Success(student);
    }

    /// <summary>`400 VALIDATION_ERROR` + `errors{}` — `ValidationBehavior` chiqishi bilan AYNAN bir shakl.</summary>
    public static Error ValidationFailure(Dictionary<string, string[]> errors) =>
        new(
            ProblemCodes.ValidationError,
            "Kiritilgan ma'lumotlar noto'g'ri.",
            new Dictionary<string, object> { ["errors"] = errors });

    public static Error PublicSpaceNotConfigured() =>
        new(ProblemCodes.PublicSpaceNotConfigured, "Ommaviy makon sozlanmagan. Administratorga murojaat qiling.");
}

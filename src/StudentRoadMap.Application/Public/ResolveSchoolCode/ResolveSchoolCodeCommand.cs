using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.ResolveSchoolCode;

/// <summary>
/// `POST /api/public/schools/resolve-code` — `docs/07` 1.1a-bo'lim: maktab kodini
/// `{ slug, accessToken }` ga aylantiradi, keyin mijoz MAVJUD `/t/{slug}?k=` oqimiga o'tadi.
///
/// Nomi `*Command` — bu so'rov o'qish bo'lsa ham MUVAFFAQIYATSIZ urinishni audit'ga yozadi
/// (`SchoolCode.ResolveFailed`), ya'ni yon ta'sir bor; `TransactionBehavior` faqat
/// `*Command` larni o'raydi va `Result.Failure` da ham commit qiladi (rollback faqat istisnoda).
/// `Code` — foydalanuvchi kiritgan XOM matn (kichik harf, defis, bo'shliq bo'lishi mumkin);
/// normalizatsiya handlerda (`SchoolEntryCode.Normalize`).
/// </summary>
public sealed record ResolveSchoolCodeCommand(
    string? Code,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<ResolveSchoolCodeResult>>;

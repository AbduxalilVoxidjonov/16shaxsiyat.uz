using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.EnableTotp;

/// <summary>
/// `POST /api/auth/totp/enable` — `docs/07-api-shartnoma.md` 2-bo'lim. Yangi sekret generatsiya
/// qiladi, darhol yoqadi va 8 ta bir martalik zaxira kod qaytaradi (faqat shu javobda, ochiq
/// matnda — keyin qayta ko'rsatilmaydi, DB'da faqat xeshi saqlanadi).
/// </summary>
public sealed record EnableTotpCommand(Guid AdminUserId) : IRequest<Result<EnableTotpResult>>;

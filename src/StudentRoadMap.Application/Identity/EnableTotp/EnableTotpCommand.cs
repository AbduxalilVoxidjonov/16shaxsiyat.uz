using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.EnableTotp;

/// <summary>
/// `POST /api/auth/totp/enable` — `docs/07-api-shartnoma.md` 2-bo'lim. Yangi sekret generatsiya
/// qiladi va uni KUTISH holatida saqlaydi (`TotpEnabled` hali `false`), javobda QR kod + sir
/// qaytaradi. 2FA faqat `POST /api/auth/totp/confirm` dan keyin yoqiladi.
/// </summary>
public sealed record EnableTotpCommand(Guid AdminUserId) : IRequest<Result<EnableTotpResult>>;

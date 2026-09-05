using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Identity.ConfirmTotp;

/// <summary>
/// `POST /api/auth/totp/confirm` — o'rnatishning IKKINCHI (yakuniy) bosqichi: foydalanuvchi
/// autentifikator ilovasidagi 6 xonali kodni yuboradi. Kod kutish holatidagi sirga mos kelsagina
/// 2FA yoqiladi va zaxira kodlar beriladi.
/// </summary>
public sealed record ConfirmTotpCommand(Guid AdminUserId, string Code) : IRequest<Result<ConfirmTotpResult>>;

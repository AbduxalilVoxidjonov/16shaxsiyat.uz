using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.Logout;

/// <summary>
/// `POST /api/auth/telegram/logout` — refresh tokenni bekor qiladi. Idempotent: token
/// topilmasa/allaqachon bekor qilingan bo'lsa ham xato bermaydi (cookie kontroller tomonidan
/// HAR DOIM tozalanadi) — `LogoutCommand` bilan bir xil naqsh.
/// </summary>
public sealed record PublicLogoutCommand(string? RawRefreshToken) : IRequest<Result>;

using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.Get;

/// <summary>
/// `GET /api/admin/public-space` — ommaviy makonning holati, biriktirilgan dasturlari,
/// sozlamalari va statistikasi (2026-09-06).
///
/// Parametrsiz: bazada AYNAN BITTA ommaviy makon bor (`ux_schools_public_space`), shu sabab
/// `id` qabul qilinmaydi — admin noto'g'ri `id` bilan boshqa makonni "ommaviy" deb ocha olmaydi.
/// </summary>
public sealed record GetPublicSpaceQuery : IRequest<Result<AdminPublicSpaceDto>>;

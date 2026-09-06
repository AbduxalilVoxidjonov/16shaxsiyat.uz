using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.AssignProgram;

/// <summary>
/// `POST /api/admin/public-space/programs/{programId}` — dasturni ommaviy makonga biriktiradi.
///
/// `SchoolId` PARAMETR SIFATIDA QABUL QILINMAYDI — makon bazada yagona va handler uni o'zi
/// topadi. Aks holda admin ommaviy bo'lim orqali IXTIYORIY maktabga biriktirish qila olardi
/// (`docs/06` — ommaviy API'da ID qabul qilmaslik tamoyilining admin tomondagi aksi).
/// </summary>
public sealed record AssignPublicSpaceProgramCommand(
    Guid ProgramId,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminPublicSpaceDto>>;

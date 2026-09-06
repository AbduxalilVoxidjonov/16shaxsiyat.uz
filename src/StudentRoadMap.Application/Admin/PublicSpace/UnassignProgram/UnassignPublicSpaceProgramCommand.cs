using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.PublicSpace.UnassignProgram;

/// <summary>
/// `DELETE /api/admin/public-space/programs/{programId}` — biriktirishni olib tashlaydi.
/// `AssignPublicSpaceProgramCommand` bilan simmetrik: `SchoolId` qabul qilinmaydi.
///
/// ⚠️ Bu MAKONNI o'chirmaydi (uni o'chirish domen darajasida taqiqlangan) — faqat dastur
/// biriktirmasini. Oxirgi dastur olib tashlansa ommaviy oqim to'xtaydi, shu sabab javobdagi
/// `Availability` darhol `NoProgramAssigned` ni ko'rsatadi va UI ogohlantiradi.
/// </summary>
public sealed record UnassignPublicSpaceProgramCommand(
    Guid ProgramId,
    Guid AdminUserId,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<AdminPublicSpaceDto>>;

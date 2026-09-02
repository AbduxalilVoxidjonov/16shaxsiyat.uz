using MediatR;
using StudentRoadMap.Application.Admin.Students;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.GetById;

/// <summary>
/// `GET /api/admin/assessments/{id}` — `docs/07-api-shartnoma.md` 3.3-bo'lim: "To'liq detal
/// (yuqoridagi `latestAssessment` shakli)". Bu ibora ANIQ — javob shakli 3.2-bo'limdagi
/// `latestAssessment` bilan BIR XIL (`{id, results, aiAnalysis, aiHistory}`,
/// `AdminLatestAssessmentDto`, `Admin.Students`da mavjud) — shu sabab bu yerda YANGI DTO
/// yaratilmaydi, mavjudi qayta ishlatiladi (`prompts/15` "AVVAL O'QI": "3.3 — javob shakllari
/// aynan"). `Id` — shu SESSIYAning o'zi (`AdminStudentProfileDto.LatestAssessment`dagi kabi
/// "eng oxirgi" emas — bu yerda `assessmentId` to'g'ridan-to'g'ri berilgan).
/// </summary>
public sealed record GetAssessmentByIdQuery(Guid Id) : IRequest<Result<AdminLatestAssessmentDto>>;

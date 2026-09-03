using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.GetById;

/// <summary>
/// `GET /api/admin/assessments/{id}` — `docs/07-api-shartnoma.md` 3.3-bo'lim.
///
/// <para>
/// Javob shakli — <see cref="AdminAssessmentDetailDto"/>: `latestAssessment` (3.2-bo'lim,
/// `AdminLatestAssessmentDto`) ning `{id, results, aiAnalysis, aiHistory}` yadrosi HARFMA-HARF
/// saqlanadi, ustiga sessiyaning o'z "sarlavhasi" (holat, vaqtlar, ishonchlilik, o'quvchi,
/// maktab, dastur) va `tests[]` qo'shiladi (2026-09-03). Bungacha bu yerda `AdminLatestAssessmentDto`
/// qayta ishlatilardi — natijada detal sahifasi to'g'ridan-to'g'ri havola bilan ochilganda
/// yarim bo'sh qolardi (sarlavha ma'lumoti faqat ro'yxatdan navigatsiya holati orqali kelardi).
/// `AdminLatestAssessmentDto`ning O'ZI o'zgarmadi — u o'quvchi profilida (3.2) ishlatilishda davom etadi.
/// </para>
/// </summary>
public sealed record GetAssessmentByIdQuery(Guid Id) : IRequest<Result<AdminAssessmentDetailDto>>;

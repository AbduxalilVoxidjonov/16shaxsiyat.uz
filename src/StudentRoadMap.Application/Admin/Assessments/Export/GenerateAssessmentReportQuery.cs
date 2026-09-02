using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Assessments.Export;

/// <summary>
/// `GET /api/admin/assessments/{id}/report.pdf` — `docs/07-api-shartnoma.md` 3.3-bo'lim,
/// `prompts/27`. Audit ro'yxatida (`docs/08` §8) PDF hisobot uchun alohida band YO'Q — faqat
/// `.xlsx` eksporti (`Export.StudentsDownloaded`) qayd etiladi, shu sabab bu Query audit
/// yozmaydi (haqiqiy yon ta'sirsiz, sof `Query`).
/// </summary>
public sealed record GenerateAssessmentReportQuery(Guid AssessmentId) : IRequest<Result<AdminAssessmentReportFileDto>>;

/// <summary>Tayyor `.pdf` fayl baytlari + nomi — controller shundan `File(...)` qaytaradi.</summary>
public sealed record AdminAssessmentReportFileDto(byte[] Content, string FileName, string ContentType);

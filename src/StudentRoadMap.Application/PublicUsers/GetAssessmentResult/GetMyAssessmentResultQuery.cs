using MediatR;
using StudentRoadMap.Application.Public.GetStudentResult;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.GetAssessmentResult;

/// <summary>
/// `GET /api/me/assessments/{id}/result` — kabinetdagi bitta sessiyaning natijasi.
///
/// **`CLAUDE.md` 8-qoidasi (`ommaviy API'da ID qabul qilinmaydi`) buzilmaydi:** qoidaning
/// maqsadi — egalik ID bilan ANIQLANMASLIGI (IDOR). Bu yerda egalik JWT (`PublicUserId`)
/// bilan isbotlanadi, `AssessmentId` esa faqat SHU foydalanuvchining sessiyalari ichidan
/// tanlash uchun. Begona (yoki mavjud bo'lmagan) ID uchun javob AYNAN BIR XIL — `404`,
/// `403` EMAS: `403` "bunday sessiya bor, lekin sizniki emas" ma'lumotini oshkor qilardi.
/// </summary>
public sealed record GetMyAssessmentResultQuery(Guid PublicUserId, Guid AssessmentId) : IRequest<Result<GetStudentResultResult?>>;

using MediatR;
using StudentRoadMap.Application.PublicUsers.Common;
using StudentRoadMap.Application.PublicUsers.GetStudentProfile;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.PublicUsers.UpdateStudentProfile;

/// <summary>
/// `PUT /api/me/profile` — anketani FAQAT saqlash, sessiya OCHILMAYDI (`docs/07` §5.1b).
///
/// Nima uchun alohida buyruq: `POST /api/me/sessions` profilni yangilaydi VA test boshlaydi.
/// Kabinetdagi "O'zgartirish" shu endpoint orqali qilingan edi — foydalanuvchi faqat
/// telefonini to'g'rilamoqchi bo'lsa ham test boshlanib ketardi (egasi ko'rgan xato,
/// 2026-09-07). Bu buyruq o'sha anketa qoidalarini (`IPublicProfileInput`,
/// `PublicStudentProfile`) AYNAN qayta ishlatadi, lekin `Assessment` yaratmaydi.
///
/// Semantika `POST /api/me/sessions` bilan BIR XIL: profil yo'q → to'liq to'plam majburiy va
/// ommaviy makonda yangi `Student` yaratiladi; bor → kelgan maydon tahrir, `null` o'zgarmasin;
/// rozilik faqat eskirgan bo'lsa talab qilinadi. Javob — yangilangan `MyStudentProfileDto`
/// (`GET /api/me/profile` bilan bir shakl).
///
/// `PublicUserId` mijozdan kelmaydi — kontroller JWT `sub` dan to'ldiradi.
/// </summary>
public sealed record UpdateStudentProfileCommand(
    Guid PublicUserId,
    string? FullName = null,
    DateOnly? BirthDate = null,
    Gender? Gender = null,
    string? Phone = null,
    bool ConsentAccepted = false,
    bool? ParentalConsent = null,
    int? Grade = null,
    string? Email = null) : IRequest<Result<MyStudentProfileDto>>, IPublicProfileInput;

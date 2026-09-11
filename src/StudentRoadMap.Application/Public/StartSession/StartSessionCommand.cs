using MediatR;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Public.StartSession;

/// <summary>
/// `POST /api/public/sessions` — `docs/07-api-shartnoma.md` 1.2-bo'lim. Anketa + sessiya ochish.
/// `IpAddress`/`UserAgent` JSON tanadan emas — bu ikkalasi mijozdan kelmaydi, mutlaqo alohida
/// wire-shape (`Api.Contracts.Public.StartSessionRequest`, `IpAddress`/`UserAgent`siz)dan
/// kontroller `HttpContext` orqali to'ldiradi (`StartSessionRequest.ToCommand`, `prompts/10`
/// tuzatish #3 — Swagger'da bu ikki maydon ko'rinmasligi/mijoz yubora olmasligi uchun). Bu yerda
/// XOM IP saqlanmaydi (`IIpHasher` orqali handler ichida xeshlanadi, `CLAUDE.md` 9-band).
/// </summary>
/// <summary>
/// P52 (2026-09-11, egasining qarori): `FullName`/`BirthDate`/`Gender`/`Grade`/`Phone`
/// endi NULLABLE — `AssessmentProgram.RegistrationMode.None` dasturida registratsiya ekrani
/// umuman ko'rsatilmaydi va bu maydonlar kelmaydi (kelsa ham e'tiborsiz qoldiriladi,
/// `StartSessionCommandHandler`). `RegistrationMode.Full` dasturda esa hamon majburiy —
/// bu tekshiruv (DB'ga bog'liqligi sabab) validator EMAS, handlerda (`ValidateRequiredIdentityFields`).
/// `ConsentAccepted` ikkala rejimda ham majburiy (huquqiy rozilik).
/// </summary>
public sealed record StartSessionCommand(
    string Slug,
    string AccessToken,
    string? AccessCode,
    string? FullName,
    DateOnly? BirthDate,
    Gender? Gender,
    int? Grade,
    string? ClassLetter,
    string? Phone,
    string? ParentPhone,
    string? Email,
    bool ConsentAccepted,
    string? LanguageCode,
    /// <summary>
    /// `docs/06` 8-bo'lim, 2026-09-02 qaror (`prompts/34` C9-band). Maktabda aynan bitta dastur
    /// mavjud bo'lsa bo'sh qoldirilishi mumkin — o'sha avtomatik tanlanadi.
    /// </summary>
    string? ProgramCode = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<StartSessionResult>>;

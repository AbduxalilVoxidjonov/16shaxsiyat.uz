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
public sealed record StartSessionCommand(
    string Slug,
    string AccessToken,
    string? AccessCode,
    string FullName,
    DateOnly BirthDate,
    Gender Gender,
    int Grade,
    string? ClassLetter,
    string Phone,
    string? ParentPhone,
    string? Email,
    bool ConsentAccepted,
    string? LanguageCode,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<StartSessionResult>>;

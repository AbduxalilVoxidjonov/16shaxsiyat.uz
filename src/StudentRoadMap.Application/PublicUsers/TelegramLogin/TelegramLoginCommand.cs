using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.TelegramLogin;

/// <summary>
/// `POST /api/auth/telegram` — Telegram Login Widget qaytargan obyekt (`docs/07` 1a-bo'lim).
/// Maydon nomlari Telegram hujjatidagi `snake_case` kalitlarga to'g'ridan-to'g'ri mos keladi
/// (wire-shape `Api.Contracts.PublicUsers.TelegramLoginRequest` `[JsonPropertyName]` bilan
/// xaritalaydi) — chunki AYNAN shu kalitlar `data_check_string` ga tushadi.
///
/// `IpAddress`/`UserAgent` mijozdan KELMAYDI — kontroller `HttpContext`dan to'ldiradi
/// (`LoginCommand`/`StartSessionCommand` bilan bir xil naqsh); ular imzoga kirmaydi.
/// </summary>
public sealed record TelegramLoginCommand(
    long Id,
    long AuthDate,
    string Hash,
    string? FirstName = null,
    string? LastName = null,
    string? Username = null,
    string? PhotoUrl = null,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<Result<TelegramLoginResult>>;

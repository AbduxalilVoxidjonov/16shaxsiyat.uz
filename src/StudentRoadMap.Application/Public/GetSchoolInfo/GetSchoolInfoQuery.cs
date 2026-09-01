using MediatR;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Public.GetSchoolInfo;

/// <summary>
/// `GET /api/public/schools/{slug}?k={accessToken}` — `docs/07-api-shartnoma.md` 1.1-bo'lim.
/// Maktab havolasi to'g'riligini tekshiradi va boshlanish ekranini to'ldiradi.
/// </summary>
public sealed record GetSchoolInfoQuery(string Slug, string AccessToken) : IRequest<Result<GetSchoolInfoResult>>;

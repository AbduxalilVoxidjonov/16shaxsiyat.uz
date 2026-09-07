namespace StudentRoadMap.Application.Public.ResolveSchoolCode;

/// <summary>
/// `docs/07` 1.1a: `{ slug, accessToken }` — mijoz shundan `/t/{slug}?k={accessToken}` quradi.
/// ID YO'Q (`CLAUDE.md` 8-qoida). Maktab NOMI ham yo'q — u keyingi qadamda
/// (`GET /api/public/schools/{slug}`) baribir keladi, bu javob faqat "eshik".
/// </summary>
public sealed record ResolveSchoolCodeResult(string Slug, string AccessToken);

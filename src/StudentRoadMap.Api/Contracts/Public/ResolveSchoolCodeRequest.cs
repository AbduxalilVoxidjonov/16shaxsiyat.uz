using StudentRoadMap.Application.Public.ResolveSchoolCode;

namespace StudentRoadMap.Api.Contracts.Public;

/// <summary>
/// `POST /api/public/schools/resolve-code` so'rov tanasi — `docs/07` 1.1a. Kod TANADA, URL'da
/// EMAS (server loglari/brauzer tarixiga tushmasin). `Code` xom matn: `7K3M-9XQ2`, `7k3m 9xq2`
/// va h.k. — normalizatsiya serverda. `IpAddress`/`UserAgent` kontroller tomonidan qo'shiladi
/// (`StartSessionRequest` bilan bir xil "wire shape" naqshi).
/// </summary>
public sealed record ResolveSchoolCodeRequest(string Code)
{
    public ResolveSchoolCodeCommand ToCommand(string? ipAddress, string? userAgent) =>
        new(Code, ipAddress, userAgent);
}

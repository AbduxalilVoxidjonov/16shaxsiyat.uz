namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// `IAiAnalysisProvider.CheckHealthAsync` javobi — superadmin panelida "ulanishni tekshirish"
/// (`docs/04-domain-model.md` 2.9-bo'lim: `AiProviderConfig.LastCheckStatus`/`LastCheckedAt`) uchun.
/// </summary>
/// <param name="IsHealthy">Provider hozir ishlayaptimi (kalit to'g'ri, tarmoq bor).</param>
/// <param name="Message">Qisqa holat tavsifi — API kalitini o'z ichiga olmaydi.</param>
public sealed record AiHealthResult(bool IsHealthy, string? Message);

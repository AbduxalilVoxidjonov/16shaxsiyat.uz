namespace StudentRoadMap.Application.Common.Interfaces;

/// <summary>
/// Joriy vaqt abstraksiyasi — `Domain` va `Application` da `DateTime.Now`/`DateTime.UtcNow`
/// bevosita ishlatilmaydi (`CLAUDE.md` 2-qoida): vaqt shu interfeys orqali uzatiladi,
/// testlarda soxtalashtiriladi (`FakeDateTime`).
/// </summary>
public interface IDateTime
{
    /// <summary>Joriy vaqt — har doim UTC (`docs/05` 1-bo'lim: hamma joyda UTC).</summary>
    DateTimeOffset UtcNow { get; }
}

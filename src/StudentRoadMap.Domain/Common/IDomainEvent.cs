namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Domen hodisasi belgisi. Application qatlamida MediatR notification'ga o'raladi.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Hodisa yuz bergan vaqt — chaqiruvchi tomonidan uzatiladi.</summary>
    DateTimeOffset OccurredAt { get; }
}

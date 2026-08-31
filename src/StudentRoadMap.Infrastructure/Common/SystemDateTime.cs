using StudentRoadMap.Application.Common.Interfaces;

namespace StudentRoadMap.Infrastructure.Common;

/// <summary>`IDateTime` ning tizim soatiga tayangan amalga oshirilishi — real muhitda ishlatiladi.</summary>
internal sealed class SystemDateTime : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

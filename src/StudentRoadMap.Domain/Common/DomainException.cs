namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Domen invarianti buzilganda otiladi (masalan, `Assessment` holat mashinasida noto'g'ri o'tish).
/// `Code` — `docs/06-arxitektura.md` 6-bo'limidagi xato kodlari uslubida (`SYSTEM_TEST_LOCKED` kabi).
/// </summary>
public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}

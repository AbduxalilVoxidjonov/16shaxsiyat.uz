using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Students;

/// <summary>
/// O'zbekiston telefon raqami — `+998XXXXXXXXX` formatiga normalizatsiya qilinadi.
/// Qabul qilinadigan kirish namunalari: `998901234567`, `901234567`, `+998 90 123 45 67`,
/// `(90) 123-45-67`, `90-123-45-67`.
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    private const int LocalDigitCount = 9;

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<PhoneNumber>(new Error("PHONE_EMPTY", "Telefon raqami bo'sh bo'lishi mumkin emas."));
        }

        var digits = ExtractDigits(raw);

        string? local9 = digits.Length switch
        {
            LocalDigitCount => digits,
            LocalDigitCount + 3 when digits.StartsWith("998", StringComparison.Ordinal) => digits[3..],
            _ => null,
        };

        if (local9 is null)
        {
            return Result.Failure<PhoneNumber>(new Error("PHONE_INVALID", "Telefon raqami noto'g'ri formatda."));
        }

        return Result.Success(new PhoneNumber($"+998{local9}"));
    }

    private static string ExtractDigits(string raw)
    {
        Span<char> buffer = stackalloc char[raw.Length];
        var count = 0;
        foreach (var ch in raw)
        {
            if (char.IsAsciiDigit(ch))
            {
                buffer[count++] = ch;
            }
        }

        return new string(buffer[..count]);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

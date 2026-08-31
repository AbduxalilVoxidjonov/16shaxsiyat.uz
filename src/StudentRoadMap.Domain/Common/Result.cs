namespace StudentRoadMap.Domain.Common;

/// <summary>
/// Muvaffaqiyat/muvaffaqiyatsizlik natijasi — istisno biznes oqimi uchun ishlatilmaydi.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Muvaffaqiyatli natijada xato bo'lishi mumkin emas.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Muvaffaqiyatsiz natijada xato ko'rsatilishi shart.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>Qiymat qaytaradigan natija.</summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Faqat <see cref="Result.IsSuccess"/> = true bo'lganda chaqiriladi.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Muvaffaqiyatsiz natijaning qiymati yo'q.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}

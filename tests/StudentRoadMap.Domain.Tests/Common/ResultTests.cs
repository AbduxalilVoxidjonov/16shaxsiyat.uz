using FluentAssertions;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithoutValue_ReturnsSuccessfulResultWithNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WithError_ReturnsFailedResultCarryingError()
    {
        var error = new Error("SOME_ERROR", "Xato yuz berdi.");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_WithNoneError_ThrowsInvalidOperationException()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SuccessGeneric_WithValue_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureGeneric_Value_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<int>(new Error("CODE", "Xabar"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitOperator_FromValue_CreatesSuccessfulResult()
    {
        Result<string> result = "salom";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("salom");
    }

    [Fact]
    public void Error_None_HasEmptyCodeAndMessage()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_Equals_ComparesByCodeAndMessage()
    {
        var a = new Error("CODE", "Xabar");
        var b = new Error("CODE", "Xabar");
        var c = new Error("CODE", "Boshqa xabar");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }
}

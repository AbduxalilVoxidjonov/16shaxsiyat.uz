using FluentAssertions;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Domain.Tests.Common;

/// <summary>
/// `Entity` va `ValueObject` bazaviy sinflarini konkret domen tiplari orqali tekshiradi
/// (`Answer` — Entity, `PhoneNumber` — ValueObject uchun qulay konkret misollar).
/// </summary>
public sealed class EntityAndValueObjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Entity_Constructor_WithEmptyGuid_ThrowsArgumentException()
    {
        var act = () => Answer.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Entity_Equals_WithSameIdAndType_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var answer1 = Answer.Create(id, Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        var answer2 = Answer.Create(id, Guid.NewGuid(), Guid.NewGuid(), 5, null, 2000, Now);

        answer1.Should().Be(answer2);
        (answer1 == answer2).Should().BeTrue();
        answer1.GetHashCode().Should().Be(answer2.GetHashCode());
    }

    [Fact]
    public void Entity_Equals_WithDifferentId_ReturnsFalse()
    {
        var answer1 = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);
        var answer2 = Answer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, null, 1000, Now);

        answer1.Should().NotBe(answer2);
        (answer1 == answer2).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_Equals_WithSameComponents_ReturnsTrue()
    {
        var phone1 = PhoneNumber.Create("901234567").Value;
        var phone2 = PhoneNumber.Create("+998 90 123 45 67").Value;

        phone1.Should().Be(phone2);
        (phone1 == phone2).Should().BeTrue();
        phone1.GetHashCode().Should().Be(phone2.GetHashCode());
    }

    [Fact]
    public void ValueObject_Equals_WithDifferentComponents_ReturnsFalse()
    {
        var phone1 = PhoneNumber.Create("901234567").Value;
        var phone2 = PhoneNumber.Create("907654321").Value;

        phone1.Should().NotBe(phone2);
        (phone1 != phone2).Should().BeTrue();
    }
}

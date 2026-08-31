using FluentAssertions;

namespace StudentRoadMap.Domain.Tests;

/// <summary>
/// Loyiha skeletini tekshiruvchi smoke test — Domain.Tests loyihasi to'g'ri
/// sozlanganini va xUnit ishga tushishini tasdiqlaydi. Haqiqiy scoring testlari
/// keyingi promptlarda (P05+) qo'shiladi.
/// </summary>
public sealed class SmokeTests
{
    [Fact]
    public void Test_infratuzilmasi_ishlaydi()
    {
        var result = 2 + 2;

        result.Should().Be(4);
    }
}

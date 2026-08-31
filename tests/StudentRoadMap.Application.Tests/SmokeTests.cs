using FluentAssertions;

namespace StudentRoadMap.Application.Tests;

/// <summary>
/// Loyiha skeletini tekshiruvchi smoke test — Application.Tests loyihasi to'g'ri
/// sozlanganini tasdiqlaydi. Haqiqiy handler testlari keyingi promptlarda qo'shiladi.
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

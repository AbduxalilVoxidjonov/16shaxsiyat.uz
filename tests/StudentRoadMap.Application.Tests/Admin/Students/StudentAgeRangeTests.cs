using FluentAssertions;
using StudentRoadMap.Application.Admin.Students.List;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Admin.Students;

/// <summary>
/// `StudentAgeRange` — yosh oralig'i → `BirthDate` chegaralari. Formulalar `Student.CalculateAge`
/// bilan AYNAN mos bo'lishi shart: bu yerda har chegara `CalculateAge` orqali qayta tekshiriladi
/// (bugun tug'ilgan kuni bo'lgan o'quvchi, kabisa yili 29-fevral).
/// </summary>
public sealed class StudentAgeRangeTests
{
    private static readonly DateOnly Today = new(2026, 9, 7);

    [Fact]
    public void LatestBirthDateInclusive_BugunTugilganKuni_MinYoshgaTolganKiradi()
    {
        var latest = StudentAgeRange.LatestBirthDateInclusive(11, Today);

        latest.Should().Be(new DateOnly(2015, 9, 7));
        Student.CalculateAge(latest, Today).Should().Be(11, "bugun 11 ga to'lgan — `age >= 11` rost");
        Student.CalculateAge(latest.AddDays(1), Today).Should().Be(10, "ertaga to'ladi — chegaradan tashqarida");
    }

    [Fact]
    public void EarliestBirthDateExclusive_BugunMaxPlusBirgaTolgan_Kirmaydi()
    {
        var earliestExclusive = StudentAgeRange.EarliestBirthDateExclusive(14, Today);

        earliestExclusive.Should().Be(new DateOnly(2011, 9, 7));
        Student.CalculateAge(earliestExclusive, Today).Should().Be(15, "bugun 15 ga to'lgan — `age <= 14` yolg'on, EKSKLYUZIV chegara");
        Student.CalculateAge(earliestExclusive.AddDays(1), Today).Should().Be(14, "ertaga 15 bo'ladi — hali 14, kiradi");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(18)]
    [InlineData(99)]
    public void Chegaralar_CalculateAgeBilanMos(int age)
    {
        var latestInclusive = StudentAgeRange.LatestBirthDateInclusive(age, Today);
        var earliestExclusive = StudentAgeRange.EarliestBirthDateExclusive(age, Today);

        // `birthDate <= latestInclusive` ⇔ `age >= min`
        Student.CalculateAge(latestInclusive, Today).Should().BeGreaterThanOrEqualTo(age);
        Student.CalculateAge(latestInclusive.AddDays(1), Today).Should().BeLessThan(age);

        // `birthDate > earliestExclusive` ⇔ `age <= max`
        Student.CalculateAge(earliestExclusive.AddDays(1), Today).Should().BeLessThanOrEqualTo(age);
        Student.CalculateAge(earliestExclusive, Today).Should().BeGreaterThan(age);
    }

    [Fact]
    public void KabisaYili_29Fevral_28FevralgaTushadi_CalculateAgeBilanBirXil()
    {
        var leapToday = new DateOnly(2028, 2, 29);

        // 2028-02-29 − 10 yil = 2018-02-28 (`DateOnly.AddYears`); 2018-02-28 da tug'ilgan bugun 10 yosh.
        var latest = StudentAgeRange.LatestBirthDateInclusive(10, leapToday);
        latest.Should().Be(new DateOnly(2018, 2, 28));
        Student.CalculateAge(latest, leapToday).Should().Be(10);
        Student.CalculateAge(latest.AddDays(1), leapToday).Should().Be(9);
    }
}

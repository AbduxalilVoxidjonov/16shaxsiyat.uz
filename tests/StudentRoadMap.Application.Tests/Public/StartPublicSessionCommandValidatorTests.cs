using FluentAssertions;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Public.StartPublicSession;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Application.Tests.Public;

/// <summary>
/// `StartPublicSessionCommandValidator` — ommaviy (maktabsiz) oqim FORMAT qoidalari. Maktab
/// oqimidan farqlari ATAYLAB sinaladi: yosh 6–99 (maktabda 6–20), sinf ixtiyoriy
/// (maktabda majburiy 1–11).
///
/// MAJBURIYLIK (yangi profilda to'liq to'plam, eskirgan rozilik, 18 yoshgacha ota-ona
/// roziligi) bu validatorda EMAS — u `Student` bazada bor-yo'qligiga bog'liq va handlerda
/// tekshiriladi (`StartPublicSessionEndpointTests`). Shu sabab bo'sh buyruq bu yerdan o'tadi.
/// </summary>
public sealed class StartPublicSessionCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    private static StartPublicSessionCommandValidator CreateValidator() => new(new FixedDateTime(Now));

    private static StartPublicSessionCommand Command(
        int birthYear = 1990,
        int? grade = null,
        bool parentalConsent = false,
        bool consentAccepted = true,
        string fullName = "Karimov Sardor Alisherovich",
        string phone = "+998901234567",
        string? email = null) =>
        new(
            Guid.NewGuid(),
            fullName,
            new DateOnly(birthYear, 1, 1),
            Gender.Male,
            phone,
            consentAccepted,
            parentalConsent,
            grade,
            email);

    [Fact]
    public void Kattalar_SinfKorsatilmagan_Toġri()
    {
        var result = CreateValidator().Validate(Command(birthYear: 1990));

        result.IsValid.Should().BeTrue("ommaviy oqimda sinf ixtiyoriy — kattalar ham test yechadi");
    }

    [Theory]
    [InlineData(1928)] // ~98 yosh
    [InlineData(2019)] // ~7 yosh
    public void YoshChegaraIchida_Toġri(int birthYear)
    {
        var result = CreateValidator().Validate(Command(birthYear: birthYear, parentalConsent: true));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(2022)] // ~4 yosh — `Student.MinAge` dan kichik
    [InlineData(1900)] // ~126 yosh — `Student.MaxAge` dan katta
    public void YoshChegaradanTashqarida_Xato(int birthYear)
    {
        var result = CreateValidator().Validate(Command(birthYear: birthYear, parentalConsent: true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StartPublicSessionCommand.BirthDate));
    }

    [Fact]
    public void MaktabOqimidaRadEtiladiganYosh_OmmaviyOqimdaQabulQilinadi()
    {
        // 35 yoshli foydalanuvchi — maktab oqimida (6–20) rad etilardi.
        var result = CreateValidator().Validate(Command(birthYear: 1991));

        result.IsValid.Should().BeTrue();
        Student.IsAgeAllowed(new DateOnly(1991, 1, 1), DateOnly.FromDateTime(Now.UtcDateTime)).Should().BeTrue();
    }

    /// <summary>
    /// Ota-ona roziligi MAJBURIYLIGI validatorda yo'q: mavjud profilda u bazada `true` bo'lishi
    /// mumkin, validator esa bazani ko'rmaydi — qoida handlerda (integratsiya testi
    /// `StartSession_ProfilYoq_VoyagaYetmagan_OtaOnaRoziligiSiz_400`).
    /// </summary>
    [Fact]
    public void OnSakkizYoshgacha_OtaOnaRoziligiSiz_ValidatordanOtadi()
    {
        var result = CreateValidator().Validate(Command(birthYear: 2011, grade: 9, parentalConsent: false));

        result.IsValid.Should().BeTrue("majburiylik handlerda — validator faqat format");
    }

    [Fact]
    public void OnSakkizYoshgacha_OtaOnaRoziligiBilan_Toġri()
    {
        var result = CreateValidator().Validate(Command(birthYear: 2011, grade: 9, parentalConsent: true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void KattaYoshli_OtaOnaRoziligiTalabQilinmaydi()
    {
        var result = CreateValidator().Validate(Command(birthYear: 1990, parentalConsent: false));

        result.IsValid.Should().BeTrue();
    }

    /// <summary>`0` — `Student.NoGrade`: mavjud profilda sinfni ANIQ "yo'q" qilish uchun ruxsat etilgan.</summary>
    [Fact]
    public void SinfNol_NoGrade_Toġri()
    {
        var result = CreateValidator().Validate(Command(grade: Student.NoGrade));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(12)]
    [InlineData(-1)]
    public void SinfChegaradanTashqarida_Xato(int grade)
    {
        var result = CreateValidator().Validate(Command(grade: grade));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StartPublicSessionCommand.Grade));
    }

    /// <summary>
    /// Rozilik MAJBURIYLIGI validatorda yo'q — mavjud profilda rozilik joriy bo'lsa u qayta
    /// so'ralmaydi; qaror handlerda (`StartSession_RozilikEskirgan_ConsentAcceptedSiz_400`).
    /// </summary>
    [Fact]
    public void Roziliksiz_ValidatordanOtadi()
    {
        var result = CreateValidator().Validate(Command(consentAccepted: false));

        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Profil bazada bo'lganda mijoz FAQAT `programCode` (yoki bo'sh tana) yuboradi —
    /// format validatori bunday buyruqni to'xtatmasligi shart.
    /// </summary>
    [Fact]
    public void FaqatPublicUserId_BoshBuyruq_ValidatordanOtadi()
    {
        var result = CreateValidator().Validate(new StartPublicSessionCommand(Guid.NewGuid(), ProgramCode: "PERSONALITY_PROFILE"));

        result.IsValid.Should().BeTrue("shaxsiy maydonlar ixtiyoriy — profil bazada bo'lsa qayta so'ralmaydi");
    }

    /// <summary>
    /// `PhoneNumber.Create` 9 xonali lokal raqamni ham qabul qiladi (`+998` qo'shib
    /// normalizatsiya qiladi) — shu sabab "noto'g'ri" misol boshqa uzunlikda bo'lishi kerak.
    /// </summary>
    [Theory]
    [InlineData("12345")]
    [InlineData("+1 555 010 9999")]
    [InlineData("abc")]
    public void TelefonNotogri_Xato(string phone)
    {
        var result = CreateValidator().Validate(Command(phone: phone));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StartPublicSessionCommand.Phone));
    }

    [Fact]
    public void FishQisqa_Xato()
    {
        var result = CreateValidator().Validate(Command(fullName: "Ali"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StartPublicSessionCommand.FullName));
    }

    [Fact]
    public void EmailNotogri_Xato()
    {
        var result = CreateValidator().Validate(Command(email: "email-emas"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(StartPublicSessionCommand.Email));
    }

    private sealed class FixedDateTime : IDateTime
    {
        public FixedDateTime(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; }
    }
}

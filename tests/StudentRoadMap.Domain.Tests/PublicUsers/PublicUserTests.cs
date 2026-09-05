using FluentAssertions;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Domain.Tests.PublicUsers;

public sealed class PublicUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private const long TelegramId = 987_654_321_012;

    private static PublicUser CreateUser() => PublicUser.Create(
        Guid.NewGuid(),
        TelegramId,
        Now,
        username: "sardor",
        firstName: "Sardor",
        lastName: "Aliyev",
        photoUrl: "https://t.me/i/userpic/320/sardor.jpg");

    [Fact]
    public void Create_ToldiradiVaqtMaydonlarini()
    {
        var user = CreateUser();

        user.TelegramId.Should().Be(TelegramId);
        user.Username.Should().Be("sardor");
        user.FirstName.Should().Be("Sardor");
        user.LastName.Should().Be("Aliyev");
        user.CreatedAt.Should().Be(Now);
        user.UpdatedAt.Should().Be(Now);
        user.LastLoginAt.Should().Be(Now, "akkaunt yaratilishi ayni paytda birinchi kirish hamdir");
        user.DeletedAt.Should().BeNull();
        user.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_TelegramMaydonlarisiz_Ishlaydi()
    {
        // Telegram `username`/`last_name`/`photo_url` ni har doim bermaydi.
        var user = PublicUser.Create(Guid.NewGuid(), TelegramId, Now);

        user.Username.Should().BeNull();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
        user.PhotoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Create_NomusbatTelegramId_ArgumentOutOfRangeOtadi(long telegramId)
    {
        var act = () => PublicUser.Create(Guid.NewGuid(), telegramId, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_JudaUzunQiymatlar_Qirqiladi()
    {
        var user = PublicUser.Create(
            Guid.NewGuid(),
            TelegramId,
            Now,
            username: new string('u', PublicUser.MaxUsernameLength + 20),
            firstName: new string('i', PublicUser.MaxNameLength + 20),
            photoUrl: new string('p', PublicUser.MaxPhotoUrlLength + 20));

        user.Username!.Length.Should().Be(PublicUser.MaxUsernameLength);
        user.FirstName!.Length.Should().Be(PublicUser.MaxNameLength);
        user.PhotoUrl!.Length.Should().Be(PublicUser.MaxPhotoUrlLength);
    }

    [Fact]
    public void Create_BoshMatnlar_NullGaAylanadi()
    {
        var user = PublicUser.Create(Guid.NewGuid(), TelegramId, Now, username: "   ", firstName: "");

        user.Username.Should().BeNull();
        user.FirstName.Should().BeNull();
    }

    [Fact]
    public void RecordLogin_ProfilniYangilaydiVaLastLoginAtniSuradi()
    {
        var user = CreateUser();
        var later = Now.AddDays(10);

        user.RecordLogin(later, username: "sardor_new", firstName: "Sardorbek", lastName: null, photoUrl: null);

        user.Username.Should().Be("sardor_new");
        user.FirstName.Should().Be("Sardorbek");
        user.LastName.Should().BeNull("Telegram maydonni bermagan bo'lsa eski qiymat saqlanmaydi — profil oynasi Telegramdagi joriy holat");
        user.LastLoginAt.Should().Be(later);
        user.UpdatedAt.Should().Be(later);
        user.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void MarkDeleted_ShaxsniAniqlovchiBarchaMaydonniTozalaydi()
    {
        var user = CreateUser();
        var deletedAt = Now.AddDays(30);

        user.MarkDeleted(deletedAt);

        user.DeletedAt.Should().Be(deletedAt);
        user.IsDeleted.Should().BeTrue();
        user.TelegramId.Should().BeNull("anonimlashtirish: Telegram ID ham shaxsiy ma'lumot");
        user.Username.Should().BeNull();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
        user.PhotoUrl.Should().BeNull();
        user.Id.Should().NotBe(Guid.Empty, "yozuv qoladi — `students.public_user_id` FK'si va test tarixi buzilmasin");
    }

    [Fact]
    public void MarkDeleted_TakrorChaqirilsa_Idempotent()
    {
        var user = CreateUser();
        user.MarkDeleted(Now.AddDays(30));

        var act = () => user.MarkDeleted(Now.AddDays(31));

        act.Should().NotThrow();
        user.DeletedAt.Should().Be(Now.AddDays(30), "birinchi o'chirish vaqti saqlanadi");
    }

    [Fact]
    public void RecordLogin_OchirilganAkkauntda_DomainExceptionOtadi()
    {
        var user = CreateUser();
        user.MarkDeleted(Now.AddDays(30));

        var act = () => user.RecordLogin(Now.AddDays(31));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("PUBLIC_USER_DELETED");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `admin_users` jadvali — `docs/05-database-schema.md` 2-bo'lim.
///
/// <para>
/// **Bilinigan tafovut (PM'ga savol, hisobotga qarang):** DDL'da `ux_admin_users_username`/
/// `ux_admin_users_email` — `lower(username)`/`lower(email)` ifodasi ustidagi unikal indeks.
/// EF Core fluent API'da xom SQL yozmasdan ifoda ustida indeks yaratib bo'lmaydi — shu sabab
/// `HasComputedColumnSql` bilan ikkita soya (shadow) hisoblangan ustun qo'shildi
/// (`username_lower`/`email_lower`) va indeks o'sha ustunlar ustida qurildi. Funksional
/// natija bir xil (katta-kichik harfga sezgir bo'lmagan unikallik), lekin ustun tarkibi
/// DDL'dagi aynan bitta ifoda-indeksdan farq qiladi.
/// </para>
/// </summary>
internal sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Username).HasMaxLength(60).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150);
        builder.Property(u => u.PasswordHash).HasMaxLength(300).IsRequired();
        // Sentinel — enum 1 dan boshlanadi (0 hech qachon domendan kelmaydi), aniq belgilab
        // "sentinel value sozlanmagan" ogohlantirishini tinchitamiz.
        builder.Property(u => u.Role).HasConversion<short>().IsRequired().HasDefaultValue(AdminRole.SuperAdmin).HasSentinel(default(AdminRole));
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.TotpSecretEncrypted);
        builder.Property(u => u.TotpEnabled).IsRequired().HasDefaultValue(false);
        // `docs/05`da yo'q — tasdiqlash bosqichi (P46) uchun qo'shildi: `enable` chaqiruvida
        // sir KUTISH holatida saqlanadi, `confirm` da (kod tekshirilgach) asosiy sirga ko'chadi.
        builder.Property(u => u.PendingTotpSecretEncrypted);
        builder.Property(u => u.PendingTotpCreatedAt);
        // `docs/05`da yo'q — P13 TOTP qayta ishlatishga qarshi himoya uchun qo'shildi (hisobotga qarang).
        builder.Property(u => u.TotpLastUsedStep);
        builder.Property(u => u.FailedLoginCount).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.LockedUntil);
        builder.Property(u => u.LastLoginAt);
        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        // `docs/05`da yo'q — QA topilmasi (`docs/13-auth-va-jwt.md`): TOTP asosiy kod poyga
        // holatiga qarshi optimistik konkurentlik. Provayderga xos `xmin` (Postgres) EMAS —
        // sinov muhiti SQLite'da `xmin` yo'q, shu sabab oddiy, portativ `uuid` ustuni
        // (`AppDbContext.SaveChangesAsync` har `Modified` saqlashda yangilaydi).
        builder.Property(u => u.ConcurrencyStamp).IsConcurrencyToken().IsRequired();

        builder.Property<string>("UsernameLower")
            .HasColumnName("username_lower")
            .HasComputedColumnSql("lower(username)", stored: true);
        builder.HasIndex("UsernameLower").IsUnique().HasDatabaseName("ux_admin_users_username");

        builder.Property<string>("EmailLower")
            .HasColumnName("email_lower")
            .HasComputedColumnSql("lower(email)", stored: true);
        builder.HasIndex("EmailLower").IsUnique().HasDatabaseName("ux_admin_users_email");
    }
}

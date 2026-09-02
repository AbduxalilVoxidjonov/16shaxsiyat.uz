using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `admin_totp_backup_codes` jadvali — `docs/05-database-schema.md`ga PM tomonidan qo'shilgan
/// (audit_logs kabi, P13 uchun yangi, kerakli jadval — `docs/08` 2-bo'lim: "8 ta bir martalik
/// zaxira kod ... xeshlangan holda saqlanadi"). Ustun/indeks nomlari `docs/05`dagi ta'rifga
/// aynan mos (`code_hash text`, `ix_admin_totp_backup_codes_admin`).
/// </summary>
internal sealed class AdminTotpBackupCodeConfiguration : IEntityTypeConfiguration<AdminTotpBackupCode>
{
    public void Configure(EntityTypeBuilder<AdminTotpBackupCode> builder)
    {
        builder.ToTable("admin_totp_backup_codes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.AdminUserId).IsRequired();
        // `varchar(300)` EMAS — `docs/05`da `text` (Postgres'da ikkalasi ham bir xil tezlikda
        // saqlanadi/ishlaydi, cheklov keraksiz — QA tuzatmasi).
        builder.Property(c => c.CodeHash).HasColumnType("text").IsRequired();
        builder.Property(c => c.UsedAt);
        builder.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(c => c.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.AdminUserId).HasDatabaseName("ix_admin_totp_backup_codes_admin");
    }
}

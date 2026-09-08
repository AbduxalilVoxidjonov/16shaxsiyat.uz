using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`public_users` jadvali — `docs/05-database-schema.md` 2-bo'lim (P47).</summary>
internal sealed class PublicUserConfiguration : IEntityTypeConfiguration<PublicUser>
{
    public void Configure(EntityTypeBuilder<PublicUser> builder)
    {
        builder.ToTable("public_users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        // `bigint` — Telegram ID'lari 32-bitga sig'masligi rasman e'lon qilingan.
        builder.Property(u => u.TelegramId);

        builder.Property(u => u.Username).HasMaxLength(PublicUser.MaxUsernameLength);
        builder.Property(u => u.FirstName).HasMaxLength(PublicUser.MaxNameLength);
        builder.Property(u => u.LastName).HasMaxLength(PublicUser.MaxNameLength);
        builder.Property(u => u.PhotoUrl).HasMaxLength(PublicUser.MaxPhotoUrlLength);

        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(u => u.LastLoginAt).IsRequired();
        builder.Property(u => u.DeletedAt);

        // Sabab/izoh (2026-09-08) — anonimlashtirish ularga TEGMAYDI, superadmin ro'yxatida
        // "nega o'chirilgan" ko'rinishi uchun ATAYLAB saqlanadi (`PublicUser.MarkDeleted`).
        builder.Property(u => u.DeletionReason).HasColumnName("deletion_reason").HasConversion<short?>();
        builder.Property(u => u.DeletionComment).HasColumnName("deletion_comment").HasMaxLength(PublicUser.MaxDeletionCommentLength);

        // `IsDeleted` — hisoblanadigan xususiyat (`DeletedAt is not null`), ustun EMAS.
        builder.Ignore(u => u.IsDeleted);

        // Unikal, lekin QISMAN: o'chirilgan (anonimlashtirilgan) yozuvlarda `telegram_id`
        // `NULL` bo'ladi — Postgres'da bir nechta NULL unikallikni buzmaydi, ya'ni bir xil
        // foydalanuvchi akkauntini o'chirib, keyin qaytadan ro'yxatdan o'ta oladi.
        builder.HasIndex(u => u.TelegramId)
            .IsUnique()
            .HasFilter("telegram_id IS NOT NULL")
            .HasDatabaseName("ux_public_users_telegram");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("ix_public_users_created")
            .IsDescending();
    }
}

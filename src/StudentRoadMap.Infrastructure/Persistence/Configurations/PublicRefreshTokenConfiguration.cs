using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Common;
using StudentRoadMap.Domain.PublicUsers;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `public_refresh_tokens` jadvali — `RefreshTokenConfiguration` (superadmin) naqshini
/// AYNAN takrorlaydi, yagona farq FK `public_user_id` (`docs/05` 2-bo'lim, P47).
/// </summary>
internal sealed class PublicRefreshTokenConfiguration : IEntityTypeConfiguration<PublicRefreshToken>
{
    public void Configure(EntityTypeBuilder<PublicRefreshToken> builder)
    {
        builder.ToTable("public_refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.PublicUserId).IsRequired();

        // 128 — superadmin jadvalidagi bilan bir xil zaxira (SHA-256 hex 64 belgi;
        // algoritm kelajakda kuchaytirilsa ustun kengaytirishsiz yetadi).
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.RevokedAt);
        builder.Property(t => t.CreatedByIpHash).HasMaxLength(TokenHash.HexLength);
        builder.Property(t => t.CreatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<PublicUser>()
            .WithMany()
            .HasForeignKey(t => t.PublicUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ux_public_refresh_tokens_hash");

        // "Bu foydalanuvchining barcha faol tokenlarini bekor qil" (qayta-ishlatish
        // aniqlanganda) — eng ko'p ishlatiladigan so'rov.
        builder.HasIndex(t => t.PublicUserId).HasDatabaseName("ix_public_refresh_tokens_user");
    }
}

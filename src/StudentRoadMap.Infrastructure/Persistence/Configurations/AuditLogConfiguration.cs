using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Identity;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`audit_logs` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        // `docs/05` DDL: `id bigserial PRIMARY KEY` — klassik `bigserial` (sequence + DEFAULT),
        // `IDENTITY` ustuni EMAS (`UseSerialColumn`, aynan DDL'ga mos).
        builder.Property(a => a.Id).UseSerialColumn();

        builder.Property(a => a.AdminUserId);
        builder.Property(a => a.Action).HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(60);
        builder.Property(a => a.EntityId);
        builder.Property(a => a.BeforeJson).HasColumnType("jsonb");
        builder.Property(a => a.AfterJson).HasColumnType("jsonb");
        builder.Property(a => a.IpHash).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(300);
        builder.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("now()");

        // `docs/05` DDL: `admin_user_id uuid REFERENCES admin_users(id)` — ON DELETE siyosati
        // ko'rsatilmagan, standart `NO ACTION`/`RESTRICT`ga teng (admin o'chirilganda audit
        // yozuvi yo'qolmasligi kerak — tarixiy iz).
        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(a => a.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.CreatedAt).IsDescending().HasDatabaseName("ix_audit_logs_created");
        builder.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("ix_audit_logs_entity");
    }
}

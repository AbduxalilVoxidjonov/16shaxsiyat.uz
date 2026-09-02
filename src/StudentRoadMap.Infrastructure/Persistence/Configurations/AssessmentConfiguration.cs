using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Schools;
using StudentRoadMap.Domain.Students;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`assessments` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.StudentId).IsRequired();
        builder.Property(a => a.SchoolId).IsRequired();
        builder.Property(a => a.ProgramId).IsRequired();
        builder.Property(a => a.SessionToken).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Status).HasConversion<short>().IsRequired().HasDefaultValue(AssessmentStatus.Draft);
        builder.Property(a => a.LanguageCode).HasMaxLength(5).IsRequired().HasDefaultValue("uz");
        builder.Property(a => a.StartedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(a => a.ExpiresAt).IsRequired();

        builder.Property(a => a.ReliabilityScore)
            .HasConversion(v => (decimal?)v, v => v != null ? (double?)v : null)
            .HasColumnType("numeric(5,2)");

        builder.Property(a => a.ReliabilityFlag).HasConversion<short?>();
        builder.Property(a => a.TotalDurationSeconds);
        builder.Property(a => a.IpHash).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(300);
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.Metadata.FindNavigation(nameof(Assessment.Tests))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(a => a.Tests)
            .WithOne()
            .HasForeignKey(t => t.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(a => a.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK ustuni hozircha nullable (yuqoridagi izoh) — Postgres'da NULL qiymatlar FK
        // cheklovini buzmaydi (standart SQL semantikasi), shu sabab ikkinchi migratsiyadan
        // OLDIN ham bu cheklov xavfsiz qo'shiladi.
        builder.HasOne<AssessmentProgram>()
            .WithMany()
            .HasForeignKey(a => a.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.SessionToken).IsUnique().HasDatabaseName("ux_assessments_token");

        builder.HasIndex(a => a.ProgramId).HasDatabaseName("ix_assessments_program");

        builder.HasIndex(a => new { a.StudentId, a.StartedAt })
            .HasDatabaseName("ix_assessments_student")
            .IsDescending(false, true);

        builder.HasIndex(a => new { a.SchoolId, a.Status })
            .HasDatabaseName("ix_assessments_school_status");

        builder.HasIndex(a => new { a.Status, a.StartedAt })
            .HasDatabaseName("ix_assessments_status_started")
            .IsDescending(false, true);
    }
}

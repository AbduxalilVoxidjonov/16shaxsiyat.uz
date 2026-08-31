using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`test_results` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
{
    public void Configure(EntityTypeBuilder<TestResult> builder)
    {
        builder.ToTable("test_results");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.AssessmentTestId).IsRequired();
        builder.Property(r => r.AssessmentId).IsRequired();
        builder.Property(r => r.TestCode).HasMaxLength(20).IsRequired();
        builder.Property(r => r.ResultCode).HasMaxLength(20);

        builder.Property(r => r.RawScoresJson).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.NormalizedScoresJson).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.LevelsJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'");
        builder.Property(r => r.FlagsJson).HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'[]'");

        builder.Property(r => r.CompositeIndex)
            .HasConversion(v => (decimal?)v, v => v != null ? (double?)v : null)
            .HasColumnType("numeric(5,2)");

        builder.Property(r => r.ScoringVersion).IsRequired().HasDefaultValue(1);
        builder.Property(r => r.TestVersion).IsRequired().HasDefaultValue(1);
        builder.Property(r => r.ComputedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<AssessmentTest>()
            .WithMany()
            .HasForeignKey(r => r.AssessmentTestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Assessment>()
            .WithMany()
            .HasForeignKey(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.AssessmentTestId).IsUnique().HasDatabaseName("ux_test_results_test");
        builder.HasIndex(r => r.AssessmentId).HasDatabaseName("ix_test_results_assessment");
        builder.HasIndex(r => new { r.TestCode, r.ResultCode }).HasDatabaseName("ix_test_results_code");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Ai;
using StudentRoadMap.Domain.Assessments;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`ai_analyses` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AiAnalysisConfiguration : IEntityTypeConfiguration<AiAnalysis>
{
    public void Configure(EntityTypeBuilder<AiAnalysis> builder)
    {
        builder.ToTable("ai_analyses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.AssessmentId).IsRequired();
        builder.Property(a => a.Provider).HasConversion<short>().IsRequired();
        builder.Property(a => a.Model).HasMaxLength(80).IsRequired();
        builder.Property(a => a.PromptVersion).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Status).HasConversion<short>().IsRequired().HasDefaultValue(AiAnalysisStatus.Pending);

        builder.Property(a => a.RequestPayloadJson).HasColumnType("jsonb");
        builder.Property(a => a.ResponseJson).HasColumnType("jsonb");
        builder.Property(a => a.Summary);
        builder.Property(a => a.PersonalityPortrait);
        builder.Property(a => a.StrengthsJson).HasColumnType("jsonb");
        builder.Property(a => a.GrowthAreasJson).HasColumnType("jsonb");
        builder.Property(a => a.RecommendationsJson).HasColumnType("jsonb");
        builder.Property(a => a.CareerSuggestionsJson).HasColumnType("jsonb");
        builder.Property(a => a.TeacherNotes);
        builder.Property(a => a.ParentNotes);
        builder.Property(a => a.AttentionFlagsJson).HasColumnType("jsonb");

        builder.Property(a => a.InputTokens);
        builder.Property(a => a.OutputTokens);
        builder.Property(a => a.EstimatedCostUsd).HasColumnType("numeric(10,6)");
        builder.Property(a => a.DurationMs);
        builder.Property(a => a.ErrorMessage).HasMaxLength(2000);
        builder.Property(a => a.AttemptNumber).IsRequired().HasDefaultValue(1);
        builder.Property(a => a.IsCurrent).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<Assessment>()
            .WithMany()
            .HasForeignKey(a => a.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.AssessmentId, a.CreatedAt })
            .HasDatabaseName("ix_ai_analyses_assessment")
            .IsDescending(false, true);

        builder.HasIndex(a => a.AssessmentId)
            .IsUnique()
            .HasFilter("is_current = true")
            .HasDatabaseName("ux_ai_analyses_current");
    }
}

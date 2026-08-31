using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence.Converters;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`assessment_tests` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AssessmentTestConfiguration : IEntityTypeConfiguration<AssessmentTest>
{
    public void Configure(EntityTypeBuilder<AssessmentTest> builder)
    {
        builder.ToTable("assessment_tests");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.AssessmentId).IsRequired();
        builder.Property(t => t.TestDefinitionId).IsRequired();
        builder.Property(t => t.Status).HasConversion<short>().IsRequired().HasDefaultValue(TestStatus.NotStarted);
        builder.Property(t => t.DisplayOrder).IsRequired();
        builder.Property(t => t.AnsweredCount).IsRequired().HasDefaultValue(0);
        builder.Property(t => t.TotalCount).IsRequired();
        builder.Property(t => t.StartedAt);
        builder.Property(t => t.CompletedAt);

        // `docs/05` §2: `question_order_json jsonb` — NULLABLE ustun (majburiy emas).
        var questionOrderProperty = builder.Property(t => t.QuestionOrder)
            .HasConversion(JsonValueConverters.GuidListConverter)
            .HasColumnName("question_order_json")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .Metadata;
        questionOrderProperty.SetValueComparer(JsonValueConverters.GuidListComparer);
        questionOrderProperty.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(AssessmentTest.Answers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Answers)
            .WithOne()
            .HasForeignKey(a => a.AssessmentTestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TestDefinition>()
            .WithMany()
            .HasForeignKey(t => t.TestDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.AssessmentId, t.TestDefinitionId })
            .IsUnique()
            .HasDatabaseName("ux_assessment_tests");
    }
}

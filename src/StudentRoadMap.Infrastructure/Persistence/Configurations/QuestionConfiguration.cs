using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Domain.Catalog.Branching;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`questions` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(q => q.TestDefinitionId).IsRequired();
        builder.Property(q => q.Code).HasMaxLength(20).IsRequired();
        builder.Property(q => q.DisplayOrder).IsRequired();
        builder.Property(q => q.TextUz).IsRequired();
        builder.Property(q => q.TextRu);
        builder.Property(q => q.TextEn);
        builder.Property(q => q.QuestionType).HasConversion<short>().IsRequired();
        builder.Property(q => q.Scale).HasMaxLength(10).IsRequired();

        builder.Property(q => q.ScaleDirection)
            .HasConversion(v => (short)v, v => v)
            .HasColumnType("smallint")
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(q => q.Weight).HasColumnType("numeric(4,2)").IsRequired().HasDefaultValue(1.0m);
        builder.Property(q => q.IsRequired).IsRequired().HasDefaultValue(true);
        builder.Property(q => q.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(q => q.IsSystem).IsRequired().HasDefaultValue(false);

        // P52 (`docs/18` §2.3/§3.2) — tarmoqlanuvchi so'rovnoma maydonlari.
        builder.Property(q => q.SectionId).HasColumnName("section_id");
        builder.Property(q => q.Placeholder).HasColumnName("placeholder").HasMaxLength(200);
        builder.Property(q => q.InputPattern).HasColumnName("input_pattern").HasMaxLength(200);
        builder.Property(q => q.MaxLength).HasColumnName("max_length");
        builder.Property(q => q.MinSelections).HasColumnName("min_selections");
        builder.Property(q => q.MaxSelections).HasColumnName("max_selections");

        builder.Property(q => q.VisibilityRule)
            .HasColumnName("visibility_rule")
            .HasColumnType("jsonb")
            .HasConversion(
                rule => VisibilityRuleJson.Serialize(rule),
                json => VisibilityRuleJson.Deserialize(json));

        builder.HasOne<QuestionSection>()
            .WithMany()
            .HasForeignKey(q => q.SectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Metadata.FindNavigation(nameof(Question.Options))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(q => q.Options)
            .WithOne()
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.Code).IsUnique().HasDatabaseName("ux_questions_code");

        builder.HasIndex(q => new { q.TestDefinitionId, q.DisplayOrder })
            .HasDatabaseName("ix_questions_test_order");

        builder.HasIndex(q => new { q.SectionId, q.DisplayOrder })
            .HasDatabaseName("ix_questions_section");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;
using StudentRoadMap.Infrastructure.Persistence.Converters;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>
/// `answers` jadvali — `docs/05-database-schema.md` 2-bo'lim, `docs/18` §2.7/§3.3 (P52
/// tarmoqlanuvchi so'rovnoma: `RawValue` kengaytirildi, `TextValue`/`SelectedValues` yangi).
/// </summary>
internal sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers", t => t.HasCheckConstraint(
            "ck_answers_shape",
            """
            (raw_value IS NOT NULL)::int
            + (text_value IS NOT NULL)::int
            + (selected_values IS NOT NULL)::int = 1
            """));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.AssessmentTestId).IsRequired();
        builder.Property(a => a.QuestionId).IsRequired();

        // `raw_value int NOT NULL` → `int NULL` — kengaytirish, destruktiv emas (`docs/18` §3.3).
        builder.Property(a => a.RawValue).HasColumnName("raw_value");
        builder.Property(a => a.TextValue).HasColumnName("text_value").HasColumnType("text").HasMaxLength(4000);

        var selectedValuesProperty = builder.Property(a => a.SelectedValues)
            .HasConversion(JsonValueConverters.AnswerSelectedValuesConverter)
            .HasColumnName("selected_values")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .Metadata;
        selectedValuesProperty.SetValueComparer(JsonValueConverters.IntListComparer);

        builder.Property(a => a.SelectedOptionId);
        builder.Property(a => a.DurationMs).IsRequired().HasDefaultValue(0);
        builder.Property(a => a.RevisionCount).IsRequired().HasDefaultValue(0);
        builder.Property(a => a.AnsweredAt).IsRequired().HasDefaultValueSql("now()");

        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AnswerOption>()
            .WithMany()
            .HasForeignKey(a => a.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.AssessmentTestId, a.QuestionId })
            .IsUnique()
            .HasDatabaseName("ux_answers_test_question");
    }
}

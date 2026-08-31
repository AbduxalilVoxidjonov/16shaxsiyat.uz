using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentRoadMap.Domain.Assessments;
using StudentRoadMap.Domain.Catalog;

namespace StudentRoadMap.Infrastructure.Persistence.Configurations;

/// <summary>`answers` jadvali — `docs/05-database-schema.md` 2-bo'lim.</summary>
internal sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answers");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.AssessmentTestId).IsRequired();
        builder.Property(a => a.QuestionId).IsRequired();
        builder.Property(a => a.RawValue).IsRequired();
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
